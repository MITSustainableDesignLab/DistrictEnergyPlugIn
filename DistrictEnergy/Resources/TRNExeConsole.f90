! TRNExeConsole: TRNSYS executable (TRNExe) as a console application
! ----------------------------------------------------------------------------------------------------------------------
!
! Calling example (standard mode):  TRNExeConsole "D:\Path to deck file\Deck file name.dck"
! Calling example (silent mode):    TRNExeConsole "D:\Path to deck file\Deck file name.dck" /s
! Silent mode runs slightly faster, it does not display the simulation progress or elapsed time
!
! Project settings that are required (status as of TRNSYS 17.1.28)
! Configuration properties / Fortran / External Procedures / Calling Convention : STDCALL, REFERENCE (/iface:stdref)
! Configuration properties / Fortran / External Procedures / String Length Argument Passing : After all arguments (seems to be the default)
! Configuration properties / Fortran / Run-time / Check Stack Frame : no (it is the default in Release mode but not in Debug mode)
! Configuration properties / Linker / System / Stack Reserve Size : 40000000 (selected by trial and error, if this number is too small long simulations fail)
!   
! Time trials on SDHW.dck for 8760 h with timestep = 0.01: 122.2 sec (no difference between release and debug), 121.6 sec without printing the current progress
! TRNExe.exe (without online plotters) takes 150 sec, or 23 % more
!
! 2013-11-09 - MKu - First version
!                    Currently this EXE will not catch "stop" errors which raise exceptions with the RaiseException routine                  
! 2016-03-17 - MKu - Added some diagnostic messages and the /s switch (silent mode, does not display simulation progress, which runs slightly faster)
!
! ----------------------------------------------------------------------------------------------------------------------
! Copyright © 2016 Michaël Kummert / Polytechnique Montréal. All rights reserved.

program TRNExeConsole

use kernel32, only: GetCurrentDirectory, GetModuleFileName,  GetProcAddress, handle, LoadLibrary, null
use, intrinsic :: iso_c_binding
use, intrinsic :: iso_fortran_env

implicit none

! String length for file names. WARNING this must be consistent with the TRNSYS DLL (TrnsysConstants.f90)
integer, parameter :: maxPathLength = 300

! Data precision. to declare a double precision number, use real(r64) :: x. to use a double precision constant in a statement, use e.g. pi = 3.141592653589723_r64
integer, parameter :: r32 = REAL32    
integer, parameter :: r64 = REAL64

! Strings for file names
character (len=maxPathLength) :: deckPath   ! Full input file name (dir + base name + extension)
character (len=maxPathLength) :: TRNExePath ! Full TRNExe.exe file name (dir + base name + extension)
character (len=maxPathLength) :: TRNDllPath ! Full TRNDll.dll file name (dir + base name + extension)
character (len=maxPathLength) :: TRNExeDir  ! Path to TRNExe.exe (and to TRNDll.dll), e.g. C:\Trnsys17\Exe\ including trailing \ file name (dir + trailing \ but without any file name)
character (len=maxPathLength) :: currentDir ! Current directory in Windows
character (len=maxPathLength) :: TrnsysRootDir  ! Path to TRNSYS root directory, e.g. C:\Trnsys17\ (dir + trailing \ but without any file name)

! Local variables
integer :: nArgs, length, status
real(r64) :: percentCompleted, percentCompletedPrinted
logical :: exists, error = .false.
logical :: silentMode = .false.
character (len=7) :: backSpace7 = char(8) // char(8) // char(8) // char(8) // char(8) // char(8) // char(8)
character (len=maxPathLength) :: flag

! Values for date time and elapsed time
integer :: dateTime(8)
integer(kind=8) :: t1, t2, clockRate
real(r64) :: elapsedTime

! Type and number of calls to TRNSYS
integer :: callType     ! Type of call to the TRNSYS routine (this variable is called icall in TRNSYS.f90)
integer :: callNo       ! Number of the call to TRNSYS (one per time step + the initial and final calls)        
integer :: nSteps       ! Total number of time steps

! Arguments used when calling TRNSYS in TRNDll
real(r64) :: parout(1000)=0.0, plotout(1000)=0.0, startTime=0.0, stopTime=0.0, timeStep=0.0
character :: labels(4000)='', titles(1500)=''

! Interface to TRNSYS and variables used when loading the DLL and finding the address of the TRNSYS procedure
abstract interface
    subroutine TRNSYS(callType,parout,plotout,labels,titles,deckn)
        use, intrinsic :: iso_fortran_env
        integer :: callType     ! Type of call to the TRNSYS routine (this variable is called icall in TRNSYS.f90)
        real(REAL64) :: parout(1000), plotout(1000)
        character :: labels(4000), titles(1500), deckn(300)
    end subroutine TRNSYS
end interface

integer(handle) :: TRNDllHandle
integer(C_INTPTR_T) :: trnsysAddress
procedure(TRNSYS), pointer :: trnsysPointer

! %%% Main Program %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

call system_clock(t1)

open (unit=6,form='formatted',carriagecontrol='fortran')   ! Required to force display without advancing to next line (http://software.intel.com/en-us/forums/topic/297876)

! --- Retrieve deck file name from command line arguments --------------------------------------------------------------

nArgs = Command_Argument_Count()
if (nArgs < 1) then
    write(*,*) 'Fatal error. Deck file name (including full path) must be provided as argument.'
    stop 1
endif
call Get_Command_Argument(1, deckPath, length, status)

! Check that deck file exists
inquire(file = deckPath, exist = exists)
if (.not. exists) then
    write(*,'("Fatal error. Input file not found. Searched for: ",a)') deckPath
    stop 10
endif

! If everything was OK with deck file, process a possible second argument
! Defined flags: /s for silent mode (does not display progress)
if (nArgs > 1) then
    call Get_Command_Argument(2, flag, length, status)
    if ( flag == '/s' ) then
        silentMode = .true.
    else
        write(*,'("Unkown option passed as parameter will be ignored. The passed string is: ",a)') flag
    endif
endif


! --- Retrieve TRNSYS root directory so that TRNDll.dll can be loaded --------------------------------------------------
!     Note: This is not always the current directory in windows!

length = GetModuleFileName(null, TRNExePath, len(TRNExePath))
TRNExeDir = TRNExePath(1:index(TRNExePath,char(0))-1) !Trim string from terminating null character
TRNExeDir = TRNExeDir(1:index(TRNExePath,'\',back=.true.)) !Find last directory separator, trim string there (leave dir separator), e.g. C:\Trnsys17\Exe\
TrnsysRootDir = TRNExeDir(1:index(TRNExeDir,'\',back=.true.)-1) ! Remove the trailing \
TrnsysRootDir = TrnsysRootDir(1:index(TrnsysRootDir,'\',back=.true.)) !Go up once more (trim string at first \, going backwards). We are in TRNSYS root directory, e.g. C:\Trnsys17\
TRNDllPath = trim(TRNExeDir) // 'TRNDll.dll'

! --- Load TRNDll.dll --------------------------------------------------------------------------------------------------

if(.not. silentMode) then
    call Date_and_time(values = dateTime)
    write(*, '(i0.4, "-", i0.2, "-", i0.2, i3.2, ":", i0.2, ":", i0.2, " - Loading TRNDll")') dateTime(1:3), dateTime(5:7)
endif

inquire(file = TRNDllPath, exist = exists)
if (.not. exists) then
    write(*,'("Fatal error. Main TRNSYS DLL (TRNDll.dll) not found. Searched for: ",a)') TRNDllPath
    stop 20
endif
TRNDllHandle = LoadLibrary(trim(TRNDllPath)//""C)
if(TRNDllHandle <= 0) then
    write(*,'("Fatal error. Main TRNSYS DLL (TRNDll.dll) could not be loaded. Attempted to load: ",a)') TRNDllPath
    write(*, *) TRNDllHandle
    stop 30
end if

! --- Get the address of the TRNSYS procedure in the DLL and transfer it to trnsysPointer ------------------------------

trnsysAddress = GetProcAddress(TRNDllHandle, "TRNSYS"C)
if(trnsysAddress <= 0) then
    write(*,'("Fatal error. TRNSYS routine not found in TRNDll")')
    stop 40
end if
call C_F_ProcPointer(transfer(trnsysAddress,C_NULL_FUNPTR), trnsysPointer) 

! --- Initialization call to TRNSYS ------------------------------------------------------------------------------------

if(.not. silentMode) then
    call Date_and_time(values = dateTime)
    write(*, '(i0.4, "-", i0.2, "-", i0.2, i3.2, ":", i0.2, ":", i0.2, " - Initializing TRNSYS")') dateTime(1:3), dateTime(5:7)
endif
callNo = 1   ! nCall is a counter of the total number of calls to TRNSYS
callType = 0 ! Initialization: pass deck file name to TRNSYS
call trnsysPointer(callType, parout, plotout, labels, titles, deckPath)

! Retrieve simulation parameters (control cards)
startTime = parout(1)
stopTime = parout(2)
timeStep = parout(3)
! Calculate number of time steps. Note that, here, initial time is counted as a time step. 
! The result is rounded without any warning - TRNSYS itself should deal with cases when (stopTime-startTime) is not an integer multiple of the time step
nSteps = nint( (stopTime-startTime) / timeStep ) + 1

! --- Call TRNSYS once per time step -----------------------------------------------------------------------------------

percentCompletedPrinted = 0.0
percentCompleted = 0.0
if(.not. silentMode) then
    call Date_and_time(values = dateTime)
    write(*, '($, i0.4, "-", i0.2, "-", i0.2, i3.2, ":", i0.2, ":", i0.2, " - Running TRNSYS simulation")') dateTime(1:3), dateTime(5:7)
endif
if(.not. silentMode) write(*, '($,". Completed: ", f5.1," %")') 0

do while ( (callType == 0) .and. (callNo < nSteps+1) )
    callNo = callNo + 1
    callType = 1

    if(.not. silentMode) then
        if(percentCompleted > percentCompletedPrinted) then
            write (*,'($, a, f5.1, " %")') backSpace7, percentCompleted
            percentCompletedPrinted = percentCompleted
        endif
        percentCompleted = ((callNo*1000)/(nSteps+1)) / 10.0_r64
    endif
    
    call trnsysPointer(callType, parout, plotout, labels, titles, deckPath)   ! should set callType to 0 if no error occurs

end do

if(.not. silentMode) then
    write (*,'($, a, f5.1, " %")') backSpace7, percentCompleted
    write(*,*)  ! Advance to next line
endif

if (callType /= 0) then
    write(*,'("Fatal error at time = ",g," - check log file for details")') (startTime + (callNo-2) * timeStep)
    error = .true.
endif

! --- Final call to TRNSYS ---------------------------------------------------------------------------------------------

if(.not. silentMode) then
    call Date_and_time(values = dateTime)
    write(*, '(i0.4, "-", i0.2, "-", i0.2, i3.2, ":", i0.2, ":", i0.2, " - Performing final call to TRNSYS")') dateTime(1:3), dateTime(5:7)
endif
callNo = callNo + 1   ! nCall is a counter of the total number of calls to TRNSYS
callType = -1 ! Final call
call trnsysPointer(callType,parout,plotout,labels,titles,deckPath)

if (callType /= 1000) then
    write(*,'("Fatal error during final call - check log file for details")')
    error = .true.
endif
    
call system_clock(t2, clockRate)
elapsedTime = dble(t2-t1) / dble(clockRate)

if (.not. error) then
    if(.not. silentMode) then
        call Date_and_time(values = dateTime)
        write(*, '(i0.4, "-", i0.2, "-", i0.2, i3.2, ":", i0.2, ":", i0.2, " - TRNSYS simulation was completed successfully. Elapsed time (seconds) = ", g)') dateTime(1:3), dateTime(5:7), elapsedTime
    endif
endif


end program TRNExeConsole