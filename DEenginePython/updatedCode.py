import pandas as pd 
import numpy as np
import matplotlib.pyplot as plt
import pdb # use pdb.set_trace()
import time
start_time = time.time()

WeatherData = pd.read_csv('DryBulbData.csv')
LoadData = pd.read_csv('Kuwait02Loads.csv')
# LoadData = pd.read_excel('Kuwait2HourlyData_IndvBlds.xlsx','Transposed')
# Loading data with Excel took 102s vs. 18s with .csv

# v04: Cleanup of file
# v05: More cleanup: Removed the electrical transmission losses from the electricity loads on scenario 04
# v06: Updated the column reassignment
# v07: Simplified change to different cities
# v08: Correcting boiler-related calcs and set condition on PLR for heat pump in cooling mode to prevent NaN
# v09: Set min PLRs, added COP calc for heat pump in heating mode, added max COPs and limiting of heat pump values
# v10: Add COP to dist boiler, removed 1.5x factor on the CCHP boiler size, prevent inf COP on chiller when denom = 0
# v11: Fix NaN for Lisbon file on heat pump cooling COP
# v12: Absorption chiller implementation work
# v13: Updated CCHP PLR for the elec chiller to use Supp Cooling / denom vs. Cooling load /denom to get min # chillers
# v14: The above update was actually wrong. Reverted code. Imp. iterations to solve for COP and PLR of the abs chiller
# v15: Cleanup of old absorption chiller code
# v16: Updating static efficiency Abs Chiller Code
# v17: Switch back to Kuwait. Absorption chiller runs with one NaN value on Hour 8 for COP and PLR
# v21: Correcting the COP calculation for the CCHP chiller and updated output files to be location-specific
# v22: Code clean. Set min PLRs, COPs for boilers and heat pumps, correct NaN potential for HP_Cooling division by 0 COP
# v23: Add emissions by type of equipment, corrected heat pump heating COP floor to 1 and fixed need for 8761-row weather data
# v24: Reformat structure of the parameter sections to match single file
# v25: Code cleanup | Removed Cost_Generation_Kuwait variable
# v26: Modify staging for heat pumps and boilers so that min number needed are enabled for each hour
# v27: Additional cleanup 
# v28: Minor code cleanup, add two additionl chiller models
# v29: Rename Cost_Natural gas to Price_Natural Gas, added comments to iloc, updated supp elec. value calc, updated effic grid
# ...................................................................

#Regional Data
Price_Electricity_Boston = 0.17
Price_Electricity_Kuwait = 0.0035 #vs ~0.13 for the actual generation cost
Price_Electricity_Lisbon = 0.25

Price_NaturalGas_Boston = 0.038225 #Dollars per kWh
Price_NaturalGas_Kuwait = 0.058020 #Dollars per kWh
Price_NaturalGas_Lisbon = 0.1 #Dollars per kWh

Effic_PowerGen_Kuwait = 0.269 #Average thermal efficiency of electrical generation in Kuwait
Effic_PowerGen_Boston = 0.334 #Find actual value
Effic_PowerGen_Lisbon = 0.482

Losses_Transmission_Boston = 0.05 #based on US average https://www.eia.gov/tools/faqs/faq.php?id=105&t=3
Losses_Transmission_Kuwait = 0.1166 #source IEA 2014
Losses_Transmission_Lisbon = 0.1055 #source IEA 2013

Emissions_ElectricGeneration_Boston = 4.44574E-4 #Metric ton CO2 per kWh produced
Emissions_ElectricGeneration_Kuwait = 7.2734E-4 #Metric ton CO2 per kWh produced
Emissions_ElectricGeneration_Lisbon = 2.8136E-4 #Metric ton CO2 per kWh produced

#Setting the City Specific Stuff
Price_Electricity = Price_Electricity_Kuwait
Price_NaturalGas = Price_NaturalGas_Kuwait
Emissions_ElectricGeneration = Emissions_ElectricGeneration_Kuwait
Effic_PowerGen = Effic_PowerGen_Kuwait
Losses_Transmission = Losses_Transmission_Kuwait

# ...................................................................

# Code below is for testing purposes <<USER INPUTS for SINGLE FILE>>
# Cost_Electricity = 0.13 #Generation cost per kWh Source: https://www.oxfordenergy.org/wpcms/wp-content/uploads/2014/04/MEP-9.pdf
# Price_NaturalGas = 0.058020 #Dollars per kWh
# Emissions_ElectricGeneration = 7.2734000E-4 #Metric ton CO2 per kWh produced
# Effic_PowerGen = 0.35 #Average thermal efficiency of electrical generation in Kuwait

Losses_Heat_Hydronic = 0.1 #Heat transfer losses from hydronic distribution system

# Internal Parameters
CW_Delta = 11 # Difference beween CWS and CWR in degrees celsius
kWhPerTherm = 29.3 #kwh/therm
Emissions_NG_Combustion_therm = 0.005302 #Metric ton CO2 per therm of NG
Emissions_NG_Combustion_kWh = Emissions_NG_Combustion_therm / kWhPerTherm
FanPwrCoolRatio = 0 # 34.0/27.0 removed because assuming fan energy ~constant for all cases

CoolThreshold = 0.1 # v26
HeatThreshold = 0.1 # v26 

# CWR Calculation Coefficients 
c1_CWR = 28.27
c2_CWR = 5.57E-04
c3_CWR = 1.63E-01

# Gas Turbine
Effic_CCHP_Electrical = 0.40
HeatRecovery = 0.8
Ratio_HeattoPower = (1 - Effic_CCHP_Electrical)*HeatRecovery/Effic_CCHP_Electrical
MinimumLoad_GasTurbine = 0.40

c1_GT = -0.00006343
c2_GT = 0.0137
c3_GT = 0.2626

# New style, low-temperature Boiler
Size_Boiler = 15 #Nominal rated capacity of the boiler [kW] (size not specifed by EnergyPlus)
Efficiency_Boiler_Nominal = 0.8
MinPLR_Boiler = 0.10

# Cubic coeffcients 
c1 = 0.83888652
c2 = 0.132579019
c3 = -0.17028503
c4 = 0.047468326

# Empirical heat pump model from Purdue for heating mode
Size_HeatPump_Heating = 3.516825 #Nominal rated capacity of the heat pump in heating mode [kW]
Power_Max_Heating = 2.164 #Max work by compressor in kW
MinPLR_HP_Heating = 0.28
MinCOP_Heating = 0.8

THot_Heating = 308.15 #35C in Kelvin
TCold_Heating = 253.15 #-20C in Kelvin
MaxCOP_Heating = THot_Heating / (THot_Heating - TCold_Heating)

# Heating-mode heat pump model coefficients
a1 = 0.0914
a2 = 0.8033
a3 = 2.5976
a4 = -3.2925
a5 = -0.8678
a6 = 0.1902
a7 = 1.4833

# Heat Pump Model, Optimized cooling from Tea Zakula: Air-to-air heat pump with evaporator outlet at 12.5C saturated
# Mitsubishi MUZ-A09NA-1 outdoor unit heat pump and a MSZ-A09NA

Size_HeatPump_Cooling = 9 # Nominal rated capacity of the heat pump in cooling mode [kW]
MinPLR_HP_Cooling = 0.1 # Set this to 0, because a min PLR of 10% results in odd behavior: 10% and 2C DB -> -799 for COP
COP_Nominal_Cooling = 3.81

THot_Cooling =  313.15 #12.5C in Kelvin based on the assumption of the constant evaporator temperature at 12.5C from Tea's work
TCold_Cooling = 285.65  #40C in Kelvin

MinCOP_Cooling = 0.0
MaxCOP_Cooling = TCold_Cooling / (THot_Cooling - TCold_Cooling)

c1_hp = 3.02E-3
c2_hp = -3.23E-1
c3_hp = 1.23E-2
c4_hp = 4.76E-1
c5_hp = -2.38E-4
c6_hp = -2.86E-4
c7_hp = -2.02E-1
c8_hp = 6.77E-4
c9_hp = 3.71E-5
c10_hp = 4.25E-6

# District Equipment Parameters 
Size_DistrictBoiler = 2500 # kw Based on 8400 MBH boiler at MP

# Chiller Option 01 from EnergyPlus Chillers.idf (Reformulated EIR Chiller Trance CVHF 2567kW/11.77COP/VSD)
# Size_ElectricCentrifugalChiller_Nominal = 2567.1 #nominal size of the chiller in kW
# MinPLR_CentrifugalChiller = 0.11 
# CoP_Chiller_Centrifugal = 11.77
# CHWS_T = 4.44 #C (40 F)

# c1_ECC_CapFunT = 3.15E-01
# c2_ECC_CapFunT = 7.67E-03
# c3_ECC_CapFunT = -4.35E-03
# c4_ECC_CapFunT = 5.00E-02
# c5_ECC_CapFunT = -1.24E-03
# c6_ECC_CapFunT = 3.16E-03

# c1_ECC_EIRFunT = 3.65E-01
# c2_ECC_EIRFunT = -3.71E-02
# c3_ECC_EIRFunT = -7.45E-04
# c4_ECC_EIRFunT = 4.59E-02
# c5_ECC_EIRFunT = 1.72E-04
# c6_ECC_EIRFunT = -3.52E-04

# c1_ECC_EIRFPLR = -2.96E-01
# c2_ECC_EIRFPLR = 2.59E-02
# c3_ECC_EIRFPLR = 3.92E-05
# c4_ECC_EIRFPLR = 7.44E-01
# c5_ECC_EIRFPLR = 3.53E-01
# c6_ECC_EIRFPLR = -2.80E-02
# c7_ECC_EIRFPLR = 0.00E+00
# c8_ECC_EIRFPLR = 2.25E-01
# c9_ECC_EIRFPLR = 0.00E+00
# c10_ECC_EIRFPLR = 0.00E+00

# Chiller Option 02 from EnergyPlus Chillers.idf (Reformulated EIR Chiller Trace CVHE 1484kW/9.96COP/VSD)
# Size_ElectricCentrifugalChiller_Nominal = 1484 #nominal size of the chiller in kW
# MinPLR_CentrifugalChiller = 0.24 
# CoP_Chiller_Centrifugal = 9.96
# CHWS_T = 4.44 #C (40 F)

# c1_ECC_CapFunT = -9.759100E-01
# c2_ECC_CapFunT = -1.446866E-01
# c3_ECC_CapFunT = -4.694254E-03
# c4_ECC_CapFunT = 1.963005E-01
# c5_ECC_CapFunT = -4.543768E-03
# c6_ECC_CapFunT = 8.998114E-03

# c1_ECC_EIRFunT = -6.183288E-01
# c2_ECC_EIRFunT = -1.151565E-01
# c3_ECC_EIRFunT = -1.663662E-04
# c4_ECC_EIRFunT = 1.539757E-01
# c5_ECC_EIRFunT = -2.390058E-03
# c6_ECC_EIRFunT = 2.818373E-03

# c1_ECC_EIRFPLR = 2.203029E-01
# c2_ECC_EIRFPLR = 9.195177E-02
# c3_ECC_EIRFPLR = 1.943558E-05
# c4_ECC_EIRFPLR = -5.815422E+00
# c5_ECC_EIRFPLR = 9.366237E+00
# c6_ECC_EIRFPLR = -9.299721E-02
# c7_ECC_EIRFPLR = 0.00E+00
# c8_ECC_EIRFPLR = -2.757689E+00
# c9_ECC_EIRFPLR = 0.00E+00
# c10_ECC_EIRFPLR = 0.00E+00

# Chiller Option 03 from EnergyPlus Chillers.idf (Reformulated EIR Chiller McQuay PEH 819kW/8.11COP/Vanes)
Size_ElectricCentrifugalChiller_Nominal = 819 #nominal size of the chiller in kW
MinPLR_CentrifugalChiller = 0.09 
CoP_Chiller_Centrifugal = 8.11
CHWS_T = 4.44 #C (40 F)

c1_ECC_CapFunT = 5.519141E-01
c2_ECC_CapFunT = 1.393287E-02
c3_ECC_CapFunT = -4.818082E-03
c4_ECC_CapFunT = 3.705684E-02
c5_ECC_CapFunT = -1.429769E-03
c6_ECC_CapFunT = 3.473993E-03

c1_ECC_EIRFunT = 4.447588E-01
c2_ECC_EIRFunT = -3.185710E-02
c3_ECC_EIRFunT = -8.260575E-04
c4_ECC_EIRFunT = 3.712567E-02
c5_ECC_EIRFunT = -4.887950E-05
c6_ECC_EIRFunT = 4.978770E-04

c1_ECC_EIRFPLR = 1.038400E-01
c2_ECC_EIRFPLR = 1.702895E-02
c3_ECC_EIRFPLR = -1.399515E-05
c4_ECC_EIRFPLR = -9.140769E-03
c5_ECC_EIRFPLR = 1.077987E+00
c6_ECC_EIRFPLR = -1.633517E-02
c7_ECC_EIRFPLR = 0.00E+00
c8_ECC_EIRFPLR = -1.811897E-01
c9_ECC_EIRFPLR = 0.00E+00
c10_ECC_EIRFPLR = 0.00E+00

# Chiller Option 04 from EnergyPlus Chillers.idf (Reformulated EIR Chiller York YT 563kW/10.61COP/Vanes)
# Size_ElectricCentrifugalChiller_Nominal = 563 #nominal size of the chiller in kW
# MinPLR_CentrifugalChiller = 0.09 
# CoP_Chiller_Centrifugal = 10.61
# CHWS_T = 4.44 #C (40 F)

# c1_ECC_CapFunT = -2.841837E-01
# c2_ECC_CapFunT = -1.006253E-01
# c3_ECC_CapFunT = -3.157589E-03
# c4_ECC_CapFunT = 1.221758E-01
# c5_ECC_CapFunT = -3.003466E-03
# c6_ECC_CapFunT = 6.704017E-03

# c1_ECC_EIRFunT = -5.308783E-01
# c2_ECC_EIRFunT = -8.364102E-02
# c3_ECC_EIRFunT = -4.054970E-03
# c4_ECC_EIRFunT = 1.347115E-01
# c5_ECC_EIRFunT = -1.805617E-03
# c6_ECC_EIRFunT = 3.054789E-03

# c1_ECC_EIRFPLR = 4.931998E+00
# c2_ECC_EIRFPLR = -2.128161E-01
# c3_ECC_EIRFPLR = 3.520769E-04
# c4_ECC_EIRFPLR =  -8.586753E+00
# c5_ECC_EIRFPLR = 1.375722E+01
# c6_ECC_EIRFPLR = 1.940510E-01
# c7_ECC_EIRFPLR = 0.00E+00
# c8_ECC_EIRFPLR = -8.859038E+00
# c9_ECC_EIRFPLR = 0.00E+00
# c10_ECC_EIRFPLR = 0.00E+00


Power_CentrifugalChiller_Nominal = Size_ElectricCentrifugalChiller_Nominal/CoP_Chiller_Centrifugal
Size_CT_Nominal = 0.011 * Size_ElectricCentrifugalChiller_Nominal

# Cooling Tower Coefficients
a1_ct = 0.8603
a2_ct = 0.2045
a3_ct = -0.0623
a4_ct = 0.0026

# Pumping energy coefficients (pumping energy as a fraction of heat delivered)
PumpCoolFraction = 0.02
PumpHeatFraction = 0.005

# Absorption Chiller
Size_AbsorptionChiller_Nominal = 231 #nominal size of the chiller in kW
CoP_Chiller_Absorption = 1.39

#CapFunT Coefficients (biquadratic)
a1_ac = -1.15E+00
a2_ac = -8.01E-02
a3_ac = -9.45E-03
a4_ac = 2.10E-01
a5_ac = -5.67E-03
a6_ac = 9.44E-03

#TeFIRFt Coefficients (biquadratic)
b1 = 1.31E+00
b2 = -1.59E-02
b3 = 7.74E-04
b4 = -1.96E-02
b5 = 3.78E-04
b6 = 5.58E-05

#TeFIRfPLR Coefficients (quadratic)
c1_ac = 2.63E-02
c2_ac = 6.78E-01
c3_ac = 2.74E-01

# ...................................................................

# Reformat imported data into columns w/ first building and associated loads to 8760, next buildings stacked underneath

NumColumns = len(LoadData.columns)
Output = None

i = 1

while i < NumColumns:
    # print i
    curr = LoadData.iloc[:, [0, i, i+1, i+2, i+3, i+4, i+5]]
    building_name = curr.columns.values[1]
    curr.rename(columns={building_name: 'Building'}, inplace=True)
    curr['Building'] = building_name.replace(':', '')
    curr.columns = [x.split('.')[0] for x in curr.columns.values]
    
    if Output is None:
        Output = curr
    else:
        Output = Output.append(curr, ignore_index=True)
        
    i += 6

Output['TotalHeating'] = Output['Heating'] + Output['DHW']
Output['Electricity'] = Output['Equipment'] + Output['Lighting']

# Output.to_csv('KuwaitReformattedInput.csv') # Use this to view the reformatted data input

Output = Output.join(WeatherData['BostonDB'], on='Hour')
Output = Output.join(WeatherData['LisbonDB'], on='Hour')
Output = Output.join(WeatherData['KuwaitDB'], on='Hour')

# ...................................................................

# Identify peak demands for cooling, heating, and electricity
PeakCooling = Output.groupby('Hour')['Cooling'].sum()
PeakCooling.sort_values(inplace=True, ascending=False)
print '\n Peak Cooling Demand (kW):', PeakCooling[:1].round() 

PeakHeating = Output.groupby('Hour')['TotalHeating'].sum()
PeakHeating.sort_values(inplace=True, ascending=False)
print '\n Peak Heating Demand (kW):', PeakHeating[:1].round()

PeakElectricity = Output.groupby('Hour')['Electricity'].sum()
PeakElectricity.sort_values(inplace=True, ascending=False)
print '\n Peak Non-HVAC Electricity Demand(kW):', PeakElectricity[:1].round()

# ...................................................................

# Identify peak demands for each building for cooling, heating, and electricity
CoolingMax = Output.groupby('Building')['Cooling'].max()
Output = Output.join(CoolingMax, on='Building', rsuffix='Max')

HeatingMax = Output.groupby('Building')['TotalHeating'].max()
Output = Output.join(HeatingMax, on='Building', rsuffix='Max')

ElectricMax = Output.groupby('Building')['Electricity'].max()
Output = Output.join(ElectricMax, on='Building', rsuffix='Max')

# ...................................................................
print "Scenario 01: Grid Supplied Electricity Satisfies All Heating, Cooling, and Electric loads"

# Determine the number of heat pumps (heating) needed for each hour, for each building
Output['NumHeatPumps_Heat'] = Output['TotalHeating']/Size_HeatPump_Heating  # v26 running heat pumps
Output['NumHeatPumps_Heat'] = np.ceil(Output['NumHeatPumps_Heat'])

# Calculate PLR by dividing hourly heating load by number of operational heat pumps.
Output['PLR_HeatPump_Heating'] = np.where(Output['TotalHeating'] <= HeatThreshold, 0, (Output['TotalHeating']/Output['NumHeatPumps_Heat'])/Size_HeatPump_Heating)
Output['PLR_HeatPump_Heating'] = np.where(Output['PLR_HeatPump_Heating'] <= MinPLR_HP_Heating, MinPLR_HP_Heating, Output['PLR_HeatPump_Heating'])

# Calculate the heat pump modifier and energy consumed by the heat pump on an hourly basis
Output['HP_Heat_Modifier'] = a1 + a2 * Output['PLR_HeatPump_Heating'] + a3 * Output['PLR_HeatPump_Heating']**2 + a4 * Output['PLR_HeatPump_Heating'] + a5 * Output['PLR_HeatPump_Heating']**3 + a6 * Output['PLR_HeatPump_Heating'] + a7 * Output['PLR_HeatPump_Heating']

Output['Energy_HeatPump_Heating'] = Output['HP_Heat_Modifier'] * Power_Max_Heating * Output['NumHeatPumps_Heat']

# Calculate the COP of the heat pump, bounded by the min and max COPs
Output['COP_HeatPump_Heating'] = np.where(Output['TotalHeating'] <= HeatThreshold, 0, Output['TotalHeating'] / Output['Energy_HeatPump_Heating']) # v23
Output['COP_HeatPump_Heating'] = np.where(Output['COP_HeatPump_Heating'] < MinCOP_Heating, MinCOP_Heating, Output['COP_HeatPump_Heating'])
Output['COP_HeatPump_Heating'] = np.where(Output['COP_HeatPump_Heating'] > MaxCOP_Heating, MaxCOP_Heating, Output['COP_HeatPump_Heating'])

# Line below ensures that the energy of the heat pump is zero, when the total heating load is below the heating threshold
Output['Energy_HeatPump_Heating'] = np.where(Output['TotalHeating'] <= HeatThreshold , 0, Output['TotalHeating']/Output['COP_HeatPump_Heating']) # added v23

# ...................................................................

# Output['NumHeatPumps_Cool'] = Output['CoolingMax']/Size_HeatPump_Cooling

# Determine the number of heat pumps (cooling) needed for each hour, for each building
Output['NumHeatPumps_Cool'] = Output['Cooling']/Size_HeatPump_Cooling # v26 update staging
Output['NumHeatPumps_Cool'] = np.ceil(Output['NumHeatPumps_Cool'])

# Calculate PLR by dividing hourly cooling load by number of operational heat pumps.
Output['PLR_HeatPump_Cooling'] = np.where(Output['NumHeatPumps_Cool'] <= 0, 0, Output['Cooling'] / (Output['NumHeatPumps_Cool']*Size_HeatPump_Cooling))
Output['PLR_HeatPump_Cooling'] = np.where(Output['PLR_HeatPump_Cooling'] < MinPLR_HP_Cooling, MinPLR_HP_Cooling, Output['PLR_HeatPump_Cooling'])

# Set the correct drybulb data <<USER INPUT>>
Output['Drybulb'] = Output['KuwaitDB']

Output['COP_HeatPump_Cooling'] = ((c1_hp + c2_hp*Output['PLR_HeatPump_Cooling'] + c3_hp*Output['Drybulb'] + 
	c4_hp*Output['PLR_HeatPump_Cooling']**2 + c5_hp*Output['PLR_HeatPump_Cooling']*Output['Drybulb'] + 
	c6_hp*Output['Drybulb']**2 + c7_hp*Output['PLR_HeatPump_Cooling']**3 + c8_hp*Output['PLR_HeatPump_Cooling']**2*Output['Drybulb'] + 
	c9_hp*Output['PLR_HeatPump_Cooling']*Output['Drybulb']**2 + c10_hp*Output['Drybulb']**3)**-1)

Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']>MaxCOP_Cooling, MaxCOP_Cooling,Output['COP_HeatPump_Cooling'])
Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling'] > MinCOP_Cooling, Output['COP_HeatPump_Cooling'], MinCOP_Cooling) # Added in v22 to set minimum COP to prevent negative values

Output['Energy_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']<=CoolThreshold,0, Output['Cooling'] / Output['COP_HeatPump_Cooling'])
Output['FanPowerSC01'] = Output['Energy_HeatPump_Cooling'] * FanPwrCoolRatio

# ...................................................................

NonHVAC_Electricity = Output['Electricity']

Load_CitySC01 = sum(NonHVAC_Electricity+Output['Energy_HeatPump_Heating']+Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01'] )

Load_Grid_SC01 = Load_CitySC01 * (1 + Losses_Transmission)
Energy_SC01 = Load_Grid_SC01/Effic_PowerGen

SC01_NonHVACEnergy = sum(NonHVAC_Electricity) * (1+Losses_Transmission) / Effic_PowerGen
SC01_HeatingEnergy = sum(Output['Energy_HeatPump_Heating']) * (1+Losses_Transmission) / Effic_PowerGen
SC01_CoolingEnergy = sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) / Effic_PowerGen
SC01_FanEnergy = sum(Output['FanPowerSC01']) * (1+Losses_Transmission) / Effic_PowerGen

SC01_AvgCoolingCOP = sum(Output['Cooling']) / SC01_CoolingEnergy # This version of COP represents the true input energy needed to meet cooling demands
SC01_AvgHeatingCOP = sum(Output['TotalHeating']) / SC01_HeatingEnergy 

print "Annual input energy for non-HVAC electricity loads:", SC01_NonHVACEnergy
print "Annual input energy for heating:", SC01_HeatingEnergy
print "Annual input energy for cooling:", SC01_CoolingEnergy
print "Annual input energy for fans:", SC01_FanEnergy

print "Annual input energy required (kWh):", Energy_SC01

print "Average Cooling COP:", SC01_AvgCoolingCOP
print "Average Heating COP:", SC01_AvgHeatingCOP

# Cost to Generate Electricity
Cost_SC01 = Load_Grid_SC01 * Price_Electricity
print "Annual cost of purchasing electricity (USD):" , Cost_SC01
print "Annual cost of purchasing natural gas (USD):" , 0.0

# Calculate CO2 Emissions Associated to Using Electricity from the Grid to Satisfy Loads
Emissions_SC01 = Load_Grid_SC01 * Emissions_ElectricGeneration
print "Annual non-HVAC carbon emissions (Metric Tons):", sum(NonHVAC_Electricity) * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual heating carbon emissions (Metric Tons):", sum(Output['Energy_HeatPump_Heating']) * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual cooling carbon emissions (Metric Tons):", sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual total carbon emissions (Metric Tons):", Emissions_SC01

Results = pd.DataFrame()
Results['Scenario01'] = SC01_NonHVACEnergy, SC01_HeatingEnergy, SC01_CoolingEnergy, Energy_SC01, SC01_AvgCoolingCOP, SC01_AvgHeatingCOP, Cost_SC01, Emissions_SC01

Output['ElectricityConsumption'] = (NonHVAC_Electricity + Output['Energy_HeatPump_Heating'] + Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01']) * (1 + Losses_Transmission)
Output['NaturalGasConsumption'] = 0.0
# ...................................................................

print "\nScenario 02: Grid-supplied Electricity Satisfies Cooling and Electric Loads and Buildings Use NG Onsite for Heating"

Load_CitySC02 = sum(NonHVAC_Electricity + Output['Energy_HeatPump_Cooling'] + Output['FanPowerSC01'])
Load_Grid_SC02 = Load_CitySC02 * (1 + Losses_Transmission)
Energy_Grid_SC02 = Load_Grid_SC02/Effic_PowerGen

# Output['NumBoilers'] = Output['TotalHeatingMax']/Size_Boiler # old version calculates total number of boilers
Output['NumBoilers'] = Output['TotalHeating']/Size_Boiler #v26 calculates number of running boilers, instead
Output['NumBoilers'] = np.ceil(Output['NumBoilers'])

# For every hour, calculate the PLR, Efficiency Boiler Modifier, and Energy Boiler
Output['PLR_Boiler'] = np.where(Output['TotalHeating'] <= HeatThreshold, 0, (Output['TotalHeating']/Output['NumBoilers'])/Size_Boiler)
Output['PLR_Boiler'] = np.where(Output['PLR_Boiler'] < MinPLR_Boiler, MinPLR_Boiler, Output['PLR_Boiler']) # Added in v22
Output['Efficiency_Boiler_Modifier'] = c1 + c2 * Output['PLR_Boiler'] + c3 * Output['PLR_Boiler']**2 + c4 * Output['PLR_Boiler']**3

Energy_NaturalGas_SC02 = sum(Output['TotalHeating'] /(Efficiency_Boiler_Nominal * Output['Efficiency_Boiler_Modifier']))

# Next four lines are just to assign an easy-to-understand variable name
SC02_NonHVACEnergy = sum(NonHVAC_Electricity) * (1+Losses_Transmission) / Effic_PowerGen
SC02_HeatingEnergy = Energy_NaturalGas_SC02
SC02_CoolingEnergy = sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) / Effic_PowerGen
SC02_FanEnergy = sum(Output['FanPowerSC01']) * (1+Losses_Transmission) / Effic_PowerGen

SC02_AvgCoolingCOP = SC01_AvgCoolingCOP
SC02_AvgHeatingCOP = sum(Output['TotalHeating']) / Energy_NaturalGas_SC02

Energy_SC02 = Energy_Grid_SC02 + Energy_NaturalGas_SC02

print "Annual input energy for non-HVAC electricity loads:", SC02_NonHVACEnergy
print "Annual input energy for heating:", SC02_HeatingEnergy
print "Annual input energy for cooling:", SC02_CoolingEnergy
print "Annual input energy for fan energy:", SC02_FanEnergy
print "Annual input energy required (kWh):", Energy_SC02

print "Average Cooling COP:", SC02_AvgCoolingCOP
print "Average Heating COP:", SC02_AvgHeatingCOP

Cost_Electricity_SC02 = Load_Grid_SC02 * Price_Electricity
Cost_NG_SC02 = Energy_NaturalGas_SC02 * Price_NaturalGas
Cost_SC02 = Cost_Electricity_SC02 + Cost_NG_SC02
print "Annual cost of purchasing electricity (USD):" , Cost_NG_SC02
print "Annual cost of purchasing NG (USD):" , Cost_Electricity_SC02
print "Annual cost of purchasing electricity and NG from grid (USD):" , Cost_SC02

Emissions_Electricity_SC02 = Load_Grid_SC02 * Emissions_ElectricGeneration
Emissions_NG_SC02 = Energy_NaturalGas_SC02 * Emissions_NG_Combustion_kWh
Emissions_SC02 = Emissions_Electricity_SC02 + Emissions_NG_SC02

print "Annual non-HVAC carbon emissions (Metric Tons):", sum(NonHVAC_Electricity) * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual heating carbon emissions (Metric Tons):", Emissions_NG_SC02
print "Annual cooling carbon emissions (Metric Tons):", sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual total carbon emissions (Metric Tons):", Emissions_SC02 # updated text in v22

Results['Scenario02'] = SC02_NonHVACEnergy, SC02_HeatingEnergy, SC02_CoolingEnergy, Energy_SC02, SC02_AvgCoolingCOP, SC02_AvgHeatingCOP, Cost_SC02, Emissions_SC02

Output['ElectricityConsumption'] = (NonHVAC_Electricity + Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01']) * (1 + Losses_Transmission)
Output['NaturalGasConsumption'] = Output['TotalHeating'] /(Efficiency_Boiler_Nominal * Output['Efficiency_Boiler_Modifier'])

# ...................................................................

print "\nScenario 03: Grid + District Heating and Cooling Plant"

Load_City_SC03 = sum(NonHVAC_Electricity)
Load_Grid_SC03 = Load_City_SC03 * (1 + Losses_Transmission)
Energy_Grid_SC03 = Load_Grid_SC03 / Effic_PowerGen

DHC = pd.DataFrame()

DHC['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
DHC['Load_HourlyHeating'] = np.where(DHC['Load_HourlyHeating']<= HeatThreshold, 0, DHC['Load_HourlyHeating']) #v27

DHC['Load_HourlyCooling'] = (Output.groupby('Hour')['Cooling'].sum()) * (1 + Losses_Heat_Hydronic)
DHC['Load_HourlyCooling'] = np.where(DHC['Load_HourlyCooling']<= CoolThreshold, 0, DHC['Load_HourlyCooling']) #v26 

DHC['Drybulb'] = WeatherData.iloc[:,3].values # Added to allow for proper joining on index | 1,2,3 for BOS, LIS, KUW

# Calculate the number of district boilers that should be enabled for each hour
# NumDistrictBoilers = np.ceil(max(PeakHeating) * (1 + Losses_Heat_Hydronic) /Size_DistrictBoiler) #v27 old code finds total # boilers
DHC['NumDistBoilers'] = np.ceil(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) / Size_DistrictBoiler) # added v27

# Calculate the PLR of the boilers using the fewest number of boilers needed to meet load for the hour
DHC['PLR_DistrictBoiler'] = np.where(DHC['Load_HourlyHeating'] <= CoolThreshold, 0, (DHC['Load_HourlyHeating'] / DHC['NumDistBoilers']) / Size_DistrictBoiler)
DHC['PLR_DistrictBoiler'] = np.where(DHC['PLR_DistrictBoiler'] < MinPLR_Boiler, MinPLR_Boiler, DHC['PLR_DistrictBoiler']) # Added in v22

# Caclulate the energy consumed by the boilers and their COP
DHC['Efficiency_DistrictBoiler_Modifier'] = c1 + c2 * DHC['PLR_DistrictBoiler'] + c3 * DHC['PLR_DistrictBoiler']**2 + c4 * DHC['PLR_DistrictBoiler']**3
DHC['Energy_DistrictBoiler'] = DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier'])
DHC['COP_DistrictBoiler'] = np.where(DHC['Energy_DistrictBoiler'] <= 0, 0, Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier']) #v28 updated equation so that it is more clear (same results)

Energy_NaturalGas_SC03 = sum(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier']))

# Calculate the mininum number of chillers needed to meet demands per hours
DHC['NumChillers'] = np.ceil(DHC['Load_HourlyCooling'] / Size_ElectricCentrifugalChiller_Nominal)
DHC['PLR'] = np.where(DHC['Load_HourlyCooling']<=CoolThreshold, 0, (DHC['Load_HourlyCooling']/DHC['NumChillers'])/Size_ElectricCentrifugalChiller_Nominal) 
DHC['PLR'] = np.where(DHC['PLR']<MinPLR_CentrifugalChiller, MinPLR_CentrifugalChiller, (DHC['Load_HourlyCooling']/DHC['NumChillers'])/Size_ElectricCentrifugalChiller_Nominal)

DHC['CWR_T'] = c1_CWR+c2_CWR*DHC['Drybulb']**2+c3_CWR*DHC['Drybulb'] * DHC['PLR']

# Calculate the coefficients necessary to estimate chiller power
DHC['CapFunT'] = c1_ECC_CapFunT+c2_ECC_CapFunT*CHWS_T + c3_ECC_CapFunT*CHWS_T**2 + c4_ECC_CapFunT*DHC['CWR_T']+c5_ECC_CapFunT*DHC['CWR_T']**2 + c6_ECC_CapFunT*CHWS_T*DHC['CWR_T']
DHC['EIRFunT'] = c1_ECC_EIRFunT+c2_ECC_EIRFunT*CHWS_T + c3_ECC_EIRFunT*CHWS_T**2 + c4_ECC_EIRFunT*DHC['CWR_T']+c5_ECC_EIRFunT*DHC['CWR_T']**2 + c6_ECC_EIRFunT*CHWS_T*DHC['CWR_T']
DHC['EIRFunPLR'] = c1_ECC_EIRFPLR+c2_ECC_EIRFPLR*DHC['CWR_T'] + c3_ECC_EIRFPLR*DHC['CWR_T']**2 + c4_ECC_EIRFPLR*DHC['PLR']+c5_ECC_EIRFPLR*DHC['PLR']**2 + c6_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']+c7_ECC_EIRFPLR*DHC['CWR_T']**3+c8_ECC_EIRFPLR*DHC['PLR']**3+c9_ECC_EIRFPLR*DHC['CWR_T']**2*DHC['PLR']+c10_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']**2

# Calculate the energy consumed by the chiller and its COP
DHC['Energy_CentrifugalChiller'] = Power_CentrifugalChiller_Nominal * DHC['CapFunT'] * DHC['EIRFunT'] * DHC['EIRFunPLR'] * DHC['NumChillers']
DHC['COP_CentrifugalChiller'] = np.where(DHC['Energy_CentrifugalChiller'] <= 0, 0, DHC['Load_HourlyCooling']/DHC['Energy_CentrifugalChiller'])

DHC['CoolingTowerPower'] = np.where(DHC['Load_HourlyCooling']<=CoolThreshold,0, Size_CT_Nominal * DHC['NumChillers'] * a1_ct * DHC['PLR']**3 + a2_ct * DHC['PLR']**2 -a3_ct * DHC['PLR'] + a4_ct)
Energy_CoolingTowers_SC03 = sum(DHC['CoolingTowerPower']) * (1 + Losses_Transmission) / Effic_PowerGen

DHC['FanPower'] = DHC['Energy_CentrifugalChiller'] * FanPwrCoolRatio
Energy_Fans_SC03 = sum(DHC['FanPower']) * (1 + Losses_Transmission) / Effic_PowerGen

#DHC['PumpPower'] = DHC['Energy_CentrifugalChiller'] * PumpPwrCoolRatio #This is based on the Australian Government stats.
DHC['PumpPower'] = DHC['Load_HourlyCooling'] * 0.02 + DHC['Load_HourlyHeating'] * (1+Losses_Heat_Hydronic) * 0.005
Energy_Pumps_SC03 = sum(DHC['PumpPower']) * (1 + Losses_Transmission) / Effic_PowerGen

Energy_CoolingElectricity_SC03 = sum(DHC['Energy_CentrifugalChiller']) * (1 + Losses_Transmission) / Effic_PowerGen
Energy_SC03 = Energy_Grid_SC03 + Energy_NaturalGas_SC03 + Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03 + Energy_CoolingTowers_SC03

SC03_AvgCoolingCOP = sum(Output['Cooling']) / ((Energy_CoolingElectricity_SC03 * (1 + Losses_Heat_Hydronic)) + Energy_CoolingTowers_SC03) # Includes chiller and CT energy in denom
SC03_AvgHeatingCOP = sum(Output['TotalHeating']) / Energy_NaturalGas_SC03 

print "Annual input energy for non-HVAC electricity loads:", Energy_Grid_SC03
print "Annual input energy for heating:", Energy_NaturalGas_SC03
print "Annual input energy for cooling:", Energy_CoolingElectricity_SC03
print "Annual input energy for cooling towers:", Energy_CoolingTowers_SC03
print "Annual input energy for fans:", Energy_Fans_SC03
print "Annual input energy for pumps:", Energy_Pumps_SC03
print "Annual input energy required (kWh):", Energy_SC03

print "Average Cooling COP:", SC03_AvgCoolingCOP
print "Average Heating COP:", SC03_AvgHeatingCOP

Cost_Electricity_SC03 = (Load_Grid_SC03 + (Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03) * Effic_PowerGen)* Price_Electricity
Cost_NG_SC03 = Energy_NaturalGas_SC03 * Price_NaturalGas
Cost_SC03 = Cost_Electricity_SC03 + Cost_NG_SC03

print "Annual cost of purchasing electricity (USD):" , Cost_Electricity_SC03
print "Annual cost of purchasing NG(USD):" , Cost_NG_SC03
print "Annual cost of purchasing electricity and NG from grid:" , Cost_SC03

Emissions_Electricity_SC03 = (Load_Grid_SC03 + (Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03) * Effic_PowerGen) * Emissions_ElectricGeneration
Emissions_NG_SC03 = Energy_NaturalGas_SC03 * Emissions_NG_Combustion_kWh
Emissions_SC03 = Emissions_Electricity_SC03 + Emissions_NG_SC03

print "Annual non-HVAC carbon emissions (Metric Tons):", Load_Grid_SC03 * Emissions_ElectricGeneration
print "Annual heating carbon emissions (Metric Tons):", Emissions_NG_SC03 + Energy_Pumps_SC03/2 * (1+Losses_Transmission) * Emissions_ElectricGeneration
print "Annual cooling carbon emissions (Metric Tons):", ((Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03/2) * Effic_PowerGen) * Emissions_ElectricGeneration
print "Annual carbon emissions (Metric Tons):", Emissions_SC03

Results['Scenario03'] = Energy_Grid_SC03, Energy_NaturalGas_SC03, Energy_CoolingElectricity_SC03, Energy_SC03, SC03_AvgCoolingCOP, SC03_AvgHeatingCOP, Cost_SC03, Emissions_SC03

DHC['ElectricityConsumption'] =  (NonHVAC_Electricity + DHC['Energy_CentrifugalChiller'] + DHC['FanPower']  + DHC['CoolingTowerPower'] + DHC['PumpPower']) * (1 + Losses_Transmission)
DHC['NaturalGasConsumption'] = DHC['Energy_DistrictBoiler']

# ...................................................................

print "\nScenario 04: CCHP Satisfies Electricity, Heating, and Cooling Demands"

CCHP = pd.DataFrame()
CCHP['Load_HourlyElectricity'] = (Output.groupby('Hour')['Electricity'].sum())
CCHP['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
CCHP['Load_HourlyHeating'] = np.where(CCHP['Load_HourlyHeating']<= HeatThreshold, 0, CCHP['Load_HourlyHeating']) #v26 

CCHP['Load_HourlyCooling'] = (Output.groupby('Hour')['Cooling'].sum()) * (1 + Losses_Heat_Hydronic)
CCHP['Load_HourlyCooling'] = np.where(CCHP['Load_HourlyCooling'] <= CoolThreshold, 0, CCHP['Load_HourlyCooling']) #v26

CCHP['Drybulb'] = WeatherData.iloc[:,3].values # v23 correcting 8761 issue | 1,2,3 for BOS, LIS, KUW
CCHP['CWR_T'] = DHC['CWR_T']
CCHP['CHWS_T'] = CHWS_T

Size_GasTurbine = max(CCHP['Load_HourlyElectricity']) #Rated electrical output of gas turbine based on the peak loads of the city

CCHP['GT_PLR'] = CCHP['Load_HourlyElectricity'] / Size_GasTurbine
CCHP['ModGT_PLR'] = np.where(CCHP['GT_PLR']<MinimumLoad_GasTurbine, MinimumLoad_GasTurbine, CCHP['GT_PLR']) #v28 Updated form

CCHP['GT_Efficiency'] = (c1_GT*(CCHP['ModGT_PLR']*100)**2+c2_GT*(CCHP['ModGT_PLR']*100)+c3_GT)*Effic_CCHP_Electrical
CCHP['Heat/Power'] = (1 - CCHP['GT_Efficiency'])*0.8 / CCHP['GT_Efficiency']
CCHP['GT_InputFuelActual'] = CCHP['ModGT_PLR'] * Size_GasTurbine / CCHP['GT_Efficiency']
Energy_GT = sum(CCHP['GT_InputFuelActual'])

CCHP['GT_InputFuelOptimal'] = CCHP['Load_HourlyElectricity'] / CCHP['GT_Efficiency']
CCHP['ExcessElectricity'] = (CCHP['GT_InputFuelActual'] - CCHP['GT_InputFuelOptimal']) * CCHP['GT_Efficiency']
ExcessElecValue = sum(CCHP['ExcessElectricity']) * Price_Electricity # updated in v29

CCHP['UseableHeat'] = CCHP['ModGT_PLR'] * CCHP['GT_Efficiency'] * Size_GasTurbine * CCHP['Heat/Power']

CCHP['HeatRemaining'] = np.where(CCHP['UseableHeat'] - CCHP['Load_HourlyHeating']<0,0,CCHP['UseableHeat'] - CCHP['Load_HourlyHeating'])
CCHP['HeatSupplementary'] = np.where(CCHP['UseableHeat'] - CCHP['Load_HourlyHeating']>=0,0,CCHP['Load_HourlyHeating'] - CCHP['UseableHeat'])
HeatSupp = sum(CCHP['HeatSupplementary'])

Size_Boiler_CCHP = np.ceil(max(CCHP['HeatSupplementary']))

CCHP['PLR_CCHPBoiler'] = np.where(CCHP['HeatSupplementary']<=0, 0, CCHP['HeatSupplementary'] / Size_Boiler_CCHP)
CCHP['PLR_CCHPBoiler'] = np.where(CCHP['PLR_CCHPBoiler'] < MinPLR_Boiler, MinPLR_Boiler, CCHP['PLR_CCHPBoiler']) # Added in v22
CCHP['Efficiency_CCHPBoiler_Modifier'] = c1 + c2 * CCHP['PLR_CCHPBoiler'] + c3 * CCHP['PLR_CCHPBoiler']**2 + c4 * CCHP['PLR_CCHPBoiler']**3

Energy_CCHPBoiler = sum(CCHP['HeatSupplementary'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * CCHP['Efficiency_CCHPBoiler_Modifier']))

CCHP['GTforHeating'] = np.where(CCHP['UseableHeat'] - CCHP['Load_HourlyHeating']<0,CCHP['UseableHeat'],CCHP['Load_HourlyHeating']) #Amount of heat from GT used for heating
InputEnergyGTforHeating = sum(CCHP['GTforHeating'])/(Ratio_HeattoPower * Effic_CCHP_Electrical) #Input energy to GTo to produce the heat needed for CCHP['GTforHeating']

# ---------------------

# 00 Initialization of COP and while loop set up ---
CCHP['COP_AbsChiller'] = CoP_Chiller_Absorption * 0.75
CCHP['PLR_AbsChiller'] = 0.7 

CCHP['COP_threshold'] = 0.01
CCHP['PLR_threshold'] = 0.01

CCHP['COP_delta'] = 0.4
CCHP['PLR_delta']  = 0.3

CCHP['COP_old'] = 0.1
CCHP['PLR_old'] = 0.1

#--------------------------

for index, row in CCHP.iterrows(): 

    while row['COP_delta'] > row['COP_threshold'] and row['PLR_delta'] > row['PLR_threshold']:

        # Calculate cooling capacity based on COP and heat remaining
        row['AbsCoolingCapacity'] = row['HeatRemaining'] * row['COP_AbsChiller']
        row['Load_AbsChiller'] = np.where(row['AbsCoolingCapacity'] >= row['Load_HourlyCooling'], row['Load_HourlyCooling'], row['AbsCoolingCapacity'])
        row['NumAbsChillers'] = np.where((row['Load_AbsChiller'] / Size_AbsorptionChiller_Nominal)<1,1, row['Load_AbsChiller'] / Size_AbsorptionChiller_Nominal)
        row['NumAbsChillers'] = np.ceil(row['NumAbsChillers'])

        # 01 PLR calculated based on initialized COP
        row['PLR_AbsChiller'] = np.where(row['AbsCoolingCapacity'] >= row['Load_HourlyCooling'], row['Load_HourlyCooling'] / (row['NumAbsChillers'] * Size_AbsorptionChiller_Nominal), row['AbsCoolingCapacity'] / (row['NumAbsChillers'] * Size_AbsorptionChiller_Nominal))
     
        # Equations needed to calculate COP
        row['CapFunT'] = a1_ac + a2_ac * CHWS_T + a3_ac * CHWS_T**2 + a4_ac * (row['CWR_T']-CW_Delta) + a5_ac * (row['CWR_T']-CW_Delta)**2 + a6_ac * CHWS_T * (row['CWR_T']-CW_Delta)
        row['TeFIRFt'] = b1 + b2*CHWS_T + b3*CHWS_T**2 + b4*(row['CWR_T']-CW_Delta) + b5 * (row['CWR_T']-CW_Delta)**2 + b6 * CHWS_T * (row['CWR_T']-CW_Delta)
        row['TeFIRfPLR'] = c1_ac + c2_ac*row['PLR_AbsChiller'] + c3_ac*row['PLR_AbsChiller']**2 
        row['X_AbsChiller'] = row['CapFunT'] * row['TeFIRFt']  * row['TeFIRfPLR'] 
        row['Energy_AbsChiller'] = Size_AbsorptionChiller_Nominal * row['X_AbsChiller'] * row['NumAbsChillers']

        # 02 COP cacluated based on PLR and the equations above
        row['COP_AbsChiller'] = row['Load_AbsChiller'] / row['Energy_AbsChiller']
        row['CoolingSupp'] = np.where(row['AbsCoolingCapacity'] >= row['Load_HourlyCooling'], 0, row['Load_HourlyCooling'] - row['AbsCoolingCapacity']) # Move outside of loop

        # Finding the difference between the current and former COP and PLR calculations
        row['COP_delta'] = abs(row['COP_AbsChiller'] - row['COP_old'])
        row['PLR_delta'] = abs(row['PLR_AbsChiller'] - row['PLR_old'])

        # Retaining the last values of COP and PLR
        row['COP_old'] = row['COP_AbsChiller'] 
        row['PLR_old'] = row['PLR_AbsChiller']

    CCHP.set_value(index,'COP_AbsChiller',row['COP_AbsChiller'])
    CCHP.set_value(index,'PLR_AbsChiller',row['PLR_AbsChiller'])
    # CCHP.set_value(index,'AbsCoolingCapacity',row['AbsCoolingCapacity'])
    CCHP.set_value(index,'CoolingSupp',row['CoolingSupp']) # This output is needed for the electric centrifugal model
    # CCHP.set_value(index,'Load_AbsChiller',row['Load_AbsChiller'])
    CCHP.set_value(index,'NumAbsChillers',row['NumAbsChillers'])
    # CCHP.set_value(index, 'X_AbsChiller',row['X_AbsChiller'])
    # CCHP.set_value(index, 'Energy_AbsChiller',row['Energy_AbsChiller'])

# ------- Constant Efficiency Absorption Chiller Code Begin ------- #
# CCHP['AbsCoolingCapacity'] = CCHP['HeatRemaining'] * CoP_Chiller_Absorption 
# CCHP['CoolingSupp'] = np.where(CCHP['AbsCoolingCapacity'] >= CCHP['Load_HourlyCooling'], 0, CCHP['Load_HourlyCooling'] - CCHP['AbsCoolingCapacity'])
# ------- Constant Efficiency Absorption Chiller Code End ------- #

# ------- Supplemental Cooling by an Electric Centrifugal Chiller Begin ------- #
NumElectricChillers = np.ceil(max(CCHP['CoolingSupp'])/Size_ElectricCentrifugalChiller_Nominal)

CCHP['NumChillers'] = np.ceil(CCHP['CoolingSupp']/Size_ElectricCentrifugalChiller_Nominal)
CCHP['PLR_ElectricChiller'] = np.where(CCHP['CoolingSupp']<=0, 0, (CCHP['CoolingSupp']/ CCHP['NumChillers'])/Size_ElectricCentrifugalChiller_Nominal)
CCHP['PLR_ElectricChiller'] = np.where(CCHP['PLR_ElectricChiller'] < MinPLR_CentrifugalChiller, MinPLR_CentrifugalChiller, np.where(CCHP['CoolingSupp']<=0, 0, (CCHP['CoolingSupp']/ CCHP['NumChillers'])/Size_ElectricCentrifugalChiller_Nominal))

CCHP['CapFunT'] = c1_ECC_CapFunT+c2_ECC_CapFunT*CHWS_T + c3_ECC_CapFunT*CHWS_T**2 + c4_ECC_CapFunT*CCHP['CWR_T']+c5_ECC_CapFunT*CCHP['CWR_T']**2 + c6_ECC_CapFunT*CHWS_T*CCHP['CWR_T']
CCHP['EIRFunT'] = c1_ECC_EIRFunT+c2_ECC_EIRFunT*CHWS_T + c3_ECC_EIRFunT*CHWS_T**2 + c4_ECC_EIRFunT*CCHP['CWR_T']+c5_ECC_EIRFunT*CCHP['CWR_T']**2 + c6_ECC_EIRFunT*CHWS_T*CCHP['CWR_T']
CCHP['EIRFunPLR'] = c1_ECC_EIRFPLR+c2_ECC_EIRFPLR*CCHP['CWR_T'] + c3_ECC_EIRFPLR*CCHP['CWR_T']**2 + c4_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']+c5_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']**2 + c6_ECC_EIRFPLR*CCHP['CWR_T']*CCHP['PLR_ElectricChiller']+c7_ECC_EIRFPLR*CCHP['CWR_T']**3+c8_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']**3+c9_ECC_EIRFPLR*CCHP['CWR_T']**2*CCHP['PLR_ElectricChiller']+c10_ECC_EIRFPLR*CCHP['CWR_T']*CCHP['PLR_ElectricChiller']**2

CCHP['Energy_CentrifugalChiller'] = Power_CentrifugalChiller_Nominal * CCHP['CapFunT'] * CCHP['EIRFunT'] * CCHP['EIRFunPLR'] * CCHP['NumChillers']
CCHP['FanPower'] = DHC['FanPower']
CCHP['PumpPower'] = DHC['PumpPower']

CCHP['COP_CentrifugalChiller'] = np.where(CCHP['Energy_CentrifugalChiller']<=0,0, CCHP['CoolingSupp'] / CCHP['Energy_CentrifugalChiller'])
CCHP['CoolingTowerPower'] = Size_CT_Nominal * CCHP['NumChillers'] * a1_ct * CCHP['PLR_ElectricChiller']**3 + a2_ct * CCHP['PLR_ElectricChiller']**2 - a3_ct * CCHP['PLR_ElectricChiller']* + a4_ct

Energy_CoolingSuppElectricity_SC04 = sum(CCHP['Energy_CentrifugalChiller']) * (1 + Losses_Transmission) / Effic_PowerGen

Energy_Fans_SC04 = Energy_Fans_SC03
Energy_Pumps_SC04 = Energy_Pumps_SC03
Energy_CoolingTowers_SC04 = Energy_CoolingTowers_SC03

Energy_SC04 = Energy_GT + Energy_CCHPBoiler + Energy_CoolingSuppElectricity_SC04 + Energy_Fans_SC04 + Energy_Pumps_SC04 + Energy_CoolingTowers_SC04

InputEnergyAbsChiller = (sum(Output['Cooling']) - sum(CCHP['CoolingSupp']))/CoP_Chiller_Absorption
SC04_AvgCoolingCOP = sum(Output['Cooling']) / (InputEnergyAbsChiller + Energy_CoolingSuppElectricity_SC04 + Energy_CoolingTowers_SC04) #Excludes pumping losses
SC04_AvgHeatingCOP = sum(Output['TotalHeating']) / (Energy_CCHPBoiler + InputEnergyGTforHeating)

print "Annual input energy for gas turbine:", Energy_GT
print "Annual input energy for supplemental heating:", Energy_CCHPBoiler
print "Annual input energy for supplemental cooling:", Energy_CoolingSuppElectricity_SC04
print "Annual input energy for cooling towers:", Energy_CoolingTowers_SC04
print "Annual input energy for fans:", Energy_Fans_SC04
print "Annual input energy for pumps:", Energy_Pumps_SC04
print "Annual input energy required (kWh):", Energy_SC04

print "Average Cooling COP:", SC04_AvgCoolingCOP
print "Average Heating COP:", SC04_AvgHeatingCOP

Cost_Electricity_SC04 = (Energy_CoolingSuppElectricity_SC04 * Effic_PowerGen) * Price_Electricity
Cost_NG_SC04 = (Energy_GT + Energy_CCHPBoiler) * Price_NaturalGas
Cost_SC04 = Cost_Electricity_SC04 + Cost_NG_SC04

print "Annual cost of purchasing electricity (USD):" , Cost_Electricity_SC04
print "Annual cost of purchasing NG(USD):" , Cost_NG_SC04
print "Annual cost (USD):" , Cost_SC04
print "Surplus electricity value (USD):" , ExcessElecValue # added v22
print "Updated annual cost (USD):" , Cost_SC04 - ExcessElecValue # added v22

Emissions_Electricity_SC04 = (Energy_CoolingSuppElectricity_SC04 * Effic_PowerGen) * Emissions_ElectricGeneration
Emissions_NG_SC04 = (Energy_GT + Energy_CCHPBoiler) * Emissions_NG_Combustion_kWh
Emissions_SC04 = Emissions_Electricity_SC04 + Emissions_NG_SC04

print "Annual gas turbine carbon emissions (Metric Tons):", sum(CCHP['GT_InputFuelActual']) * Emissions_NG_Combustion_kWh
print "Annual carbon emissions (Metric Tons):", Emissions_SC04

# Results['Scenario04'] = Energy_Grid_SC03, Energy_NaturalGas_SC03, Energy_CoolingElectricity_SC03, Energy_SC03, SC03_AvgCoolingCOP, SC03_AvgHeatingCOP, Cost_SC03, Emissions_SC03

CCHP['ElectricityConsumption'] =  (CCHP['Energy_CentrifugalChiller'] + CCHP['FanPower'] + CCHP['CoolingTowerPower'] + CCHP['PumpPower']) * (1 + Losses_Transmission)
CCHP['NaturalGasConsumption'] = CCHP['HeatSupplementary'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * CCHP['Efficiency_CCHPBoiler_Modifier']) + CCHP['GT_InputFuelActual']

# ...................................................................
# Results for Export

# Results = pd.DataFrame()

# ...................................................................
# Export Dataframes

# Output.to_csv('KuwaitGroup02_DataExport.csv')
# DHC.to_csv('KuwaitGroup02_DHCExport.csv')
# CCHP.to_csv('KuwaitGroup02_CCHPExport.csv')

print(" %s seconds " % (time.time() - start_time))
# pdb.set_trace()