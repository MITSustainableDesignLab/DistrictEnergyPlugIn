import pandas as pd 
import numpy as np
import sys
import os
import errno
import tqdm

# ...................................................................

# > > > Load Input Data [DYNAMIC INPUTS] < < <
LoadData = pd.read_csv('C:/UMI/temp/Loads.csv')
Output = LoadData #Reformatted Kuwait output to bypass the reformat section
WeatherData = pd.read_csv('C:/UMI/temp/DryBulbData.csv', delimiter=',', encoding="utf-8")

# > > > User-specified Parameters [DYNAMIC INPUTS] < < < 
Cost_Electricity = float(sys.argv[2]) #Generation cost per kWh Source: https://www.oxfordenergy.org/wpcms/wp-content/uploads/2014/04/MEP-9.pdf
Price_NaturalGas = float(sys.argv[3]) #Dollars per kWh
Emissions_ElectricGeneration = float(sys.argv[4]) #Metric ton CO2 per kWh produced
Effic_PowerGen = float(sys.argv[7]) #Average thermal efficiency of electrical generation in Kuwait
Losses_Transmission = float(sys.argv[5]) #Electrical transmission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3
Losses_Heat_Hydronic = float(sys.argv[6]) #Heat transfer losses from hydronic distribution systemission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3

# Code below is for testing purposes
# Cost_Electricity = 0.13 #Generation cost per kWh Source: https://www.oxfordenergy.org/wpcms/wp-content/uploads/2014/04/MEP-9.pdf
# Price_NaturalGas = 0.058020 #Dollars per kWh
# Emissions_ElectricGeneration = 7.2734000E-4 #Metric ton CO2 per kWh produced
# Effic_PowerGen = 0.35 #Average thermal efficiency of electrical generation in Kuwait
# Losses_Transmission = 0.05 #Electrical transmission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3
# Losses_Heat_Hydronic = 0.1 #Heat transfer losses from hydronic distribution system

# ...................................................................

# Internal Parameters
CW_Delta = 11 # Difference beween CWS and CWR in degrees celsius
kWhPerTherm = 29.3 #kwh/therm
Emissions_NG_Combustion_therm = 0.005302 #Metric ton CO2 per therm of NG
Emissions_NG_Combustion_kWh = Emissions_NG_Combustion_therm / kWhPerTherm
FanPwrCoolRatio = 0 # 34.0/27.0 removed because assuming fan energy ~constant for all cases

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
MinCOP_Heating = 1

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
TCold_Cooling = 285.65 #40C in Kelvin

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
Size_ElectricCentrifugalChiller_Nominal = 2567.1 #nominal size of the chiller in kW
MinPLR_CentrifugalChiller = 0.11 
CoP_Chiller_Centrifugal = 11.77
CHWS_T = 4.44 #C (40 F)
Power_CentrifugalChiller_Nominal = Size_ElectricCentrifugalChiller_Nominal/CoP_Chiller_Centrifugal

c1_ECC_CapFunT = 3.15E-01
c2_ECC_CapFunT = 7.67E-03
c3_ECC_CapFunT = -4.35E-03
c4_ECC_CapFunT = 5.00E-02
c5_ECC_CapFunT = -1.24E-03
c6_ECC_CapFunT = 3.16E-03

c1_ECC_EIRFunT = 3.65E-01
c2_ECC_EIRFunT = -3.71E-02
c3_ECC_EIRFunT = -7.45E-04
c4_ECC_EIRFunT = 4.59E-02
c5_ECC_EIRFunT = 1.72E-04
c6_ECC_EIRFunT = -3.52E-04

c1_ECC_EIRFPLR = -2.96E-01
c2_ECC_EIRFPLR = 2.59E-02
c3_ECC_EIRFPLR = 3.92E-05
c4_ECC_EIRFPLR = 7.44E-01
c5_ECC_EIRFPLR = 3.53E-01
c6_ECC_EIRFPLR = -2.80E-02
c7_ECC_EIRFPLR = 0.00E+00
c8_ECC_EIRFPLR = 2.25E-01
c9_ECC_EIRFPLR = 0.00E+00
c10_ECC_EIRFPLR = 0.00E+00

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

# Update Output dataframe
Output['TotalHeating'] = Output['SDL/Heating'] + Output['SDL/Domestic Hot Water']
Output['Electricity'] = Output['SDL/Equipment'] + Output['SDL/Lighting']
Output = Output.join(WeatherData.set_index('Hour'), on='Hour')

# ...................................................................

# Identify peak demands for cooling, heating, and electricity
PeakCooling = Output.groupby('Hour')['SDL/Cooling'].sum()
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
CoolingMax = Output.groupby('Building')['SDL/Cooling'].max()
Output = Output.join(CoolingMax, on='Building', rsuffix='Max')

print 'Cooling max :', CoolingMax[:1]

HeatingMax = Output.groupby('Building')['TotalHeating'].max()
Output = Output.join(HeatingMax, on='Building', rsuffix='Max')

ElectricMax = Output.groupby('Building')['Electricity'].max()
Output = Output.join(ElectricMax, on='Building', rsuffix='Max')

# Make directory for saving files
def mkdir_p(path):
    try:
        os.makedirs(path)
    except OSError as exc:  # Python >2.5
        if exc.errno == errno.EEXIST and os.path.isdir(path):
            pass
        else:
            raise

def sc01():
	    # ...................................................................
    print "Scenario 01: Grid Supplied Electricity Satisfies All Heating, Cooling, and Electric loads"

    # Calculate total heating energy needed

    Output['NumHeatPumps_Heat'] = Output['TotalHeatingMax']/Size_HeatPump_Heating 
    Output['NumHeatPumps_Heat'] = np.ceil(Output['NumHeatPumps_Heat'])
    Output['PLR_HeatPump_Heating'] = np.where(Output['TotalHeating']<=0, 0, (Output['TotalHeating']/Output['NumHeatPumps_Heat'])/Size_HeatPump_Heating)
    Output['PLR_HeatPump_Heating'] = np.where(Output['PLR_HeatPump_Heating']<=MinPLR_HP_Heating, MinPLR_HP_Heating, (Output['TotalHeating']/Output['NumHeatPumps_Heat'])/Size_HeatPump_Heating)
    Output['HP_Heat_Modifier'] = a1 + a2 * Output['PLR_HeatPump_Heating'] + a3 * Output['PLR_HeatPump_Heating']**2 + a4 * Output['PLR_HeatPump_Heating'] + a5 * Output['PLR_HeatPump_Heating']**3 + a6 * Output['PLR_HeatPump_Heating'] + a7 * Output['PLR_HeatPump_Heating']
    Output['Energy_HeatPump_Heating'] = Output['HP_Heat_Modifier'] * Power_Max_Heating * Output['NumHeatPumps_Heat']
    Output['COP_HeatPump_Heating'] = np.where(Output['TotalHeating'] <= 0, 0, Output['TotalHeating'] / Output['Energy_HeatPump_Heating']) # v23
    Output['COP_HeatPump_Heating'] = np.where(Output['COP_HeatPump_Heating'] < MinCOP_Heating,MinCOP_Heating, Output['TotalHeating'] / Output['Energy_HeatPump_Heating'])
    Output['COP_HeatPump_Heating'] = np.where(Output['COP_HeatPump_Heating'] > MaxCOP_Heating, MaxCOP_Heating, Output['COP_HeatPump_Heating'])
 
    Output['Energy_HeatPump_Heating'] = np.where(Output['TotalHeating']<=0 , 0, Output['TotalHeating']/Output['COP_HeatPump_Heating']) # added v23       # ...................................................................

    # Calculate total cooling energy needed
    Output['NumHeatPumps_Cool'] = Output['SDL/CoolingMax']/Size_HeatPump_Cooling
    Output['NumHeatPumps_Cool'] = np.ceil(Output['NumHeatPumps_Cool'])
    Output['PLR_HeatPump_Cooling'] = np.where(Output['NumHeatPumps_Cool']<=0.0,0.0, Output['SDL/Cooling'] / (Output['NumHeatPumps_Cool']*Size_HeatPump_Cooling))
    Output['PLR_HeatPump_Cooling'] = np.where(Output['PLR_HeatPump_Cooling'] < MinPLR_HP_Cooling, MinPLR_HP_Cooling, Output['PLR_HeatPump_Cooling'])

    Output['COP_HeatPump_Cooling'] = ((c1_hp + c2_hp*Output['PLR_HeatPump_Cooling'] + c3_hp*Output['DB'] + 
	    c4_hp*Output['PLR_HeatPump_Cooling']**2 + c5_hp*Output['PLR_HeatPump_Cooling']*Output['DB'] + 
	    c6_hp*Output['DB']**2 + c7_hp*Output['PLR_HeatPump_Cooling']**3 + c8_hp*Output['PLR_HeatPump_Cooling']**2*Output['DB'] + 
	    c9_hp*Output['PLR_HeatPump_Cooling']*Output['DB']**2 + c10_hp*Output['DB']**3)**-1)

    Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']>MaxCOP_Cooling, MaxCOP_Cooling,Output['COP_HeatPump_Cooling'])
    Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling'] > MinCOP_Cooling, Output['COP_HeatPump_Cooling'], MinCOP_Cooling) # Added in v22 to set minimum COP to prevent negative values

    Output['Energy_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']<=0,0, Output['SDL/Cooling'] / Output['COP_HeatPump_Cooling'])
    Output['FanPowerSC01'] = Output['Energy_HeatPump_Cooling'] * FanPwrCoolRatio

    # ...................................................................

    NonHVAC_Electricity = Output['Electricity']
    Load_CitySC01 = sum(NonHVAC_Electricity+Output['Energy_HeatPump_Heating']+Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01'] )
    Load_Grid_SC01 = Load_CitySC01 * (1 + Losses_Transmission)
    Energy_SC01 = Load_Grid_SC01/Effic_PowerGen

    SC01_NonHVACEnergy = sum(NonHVAC_Electricity) * (1+Losses_Transmission) / Effic_PowerGen
    SC01_HeatingEnergy = sum(Output['Energy_HeatPump_Heating']) * (1+Losses_Transmission) / Effic_PowerGen
    Output['SourceHeatingEnergy'] = Output['Energy_HeatPump_Heating'] * (1+Losses_Transmission) / Effic_PowerGen
    SC01_CoolingEnergy = sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) / Effic_PowerGen
    SC01_FanEnergy = sum(Output['FanPowerSC01']) * (1+Losses_Transmission) / Effic_PowerGen

    SC01_AvgCoolingCOP = sum(Output['SDL/Cooling']) / SC01_CoolingEnergy # This version of COP represents the true input energy needed to meet cooling demands
    SC01_AvgHeatingCOP = sum(Output['TotalHeating']) / SC01_HeatingEnergy 

    print "Annual input energy for non-HVAC electricity loads:", SC01_NonHVACEnergy
    print "Annual input energy for heating:", SC01_HeatingEnergy
    print "Annual input energy for cooling:", SC01_CoolingEnergy
    print "Annual input energy for fans:", SC01_FanEnergy
    print "Annual input energy required (kWh):", Energy_SC01
    print "Average Cooling COP:", SC01_AvgCoolingCOP
    print "Average Heating COP:", SC01_AvgHeatingCOP

    # Cost to Generate Electricity
    Cost_SC01 = Load_Grid_SC01 * Cost_Electricity
    print "Annual cost of generating electricity for grid (USD):" , Cost_SC01

    # Calculate CO2 Emissions Associated to Using Electricity from the Grid to Satisfy Loads
    Emissions_SC01 = Load_Grid_SC01 * Emissions_ElectricGeneration
    print "Annual carbon emissions (Metric Tons):", Emissions_SC01

    Output['ElectricityConsumption01'] = (NonHVAC_Electricity + Output['Energy_HeatPump_Heating'] + Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01']) * (1 + Losses_Transmission)
    Output['NaturalGasConsumption01'] = 0.0

    Results = pd.DataFrame()
    Results['ElectricityConsumption01'] = Output.groupby('Hour')['ElectricityConsumption01'].sum()
    Results['NaturalGasConsumption01'] = Output.groupby('Hour')['NaturalGasConsumption01'].sum()

    # Results['Scenario01'] = SC01_NonHVACEnergy, SC01_HeatingEnergy, SC01_CoolingEnergy, Energy_SC01, SC01_AvgCoolingCOP, SC01_AvgHeatingCOP, Cost_SC01, Emissions_SC01

    # Export Data
    mkdir_p('C:/UMI/temp/DHSimulationResults')
    Output.to_csv('C:/UMI/temp/DHSimulationResults/SC01_Export_OutputDataFrame.csv')
    Results.to_csv('C:/UMI/temp/DHSimulationResults/SC01_Export_ResultsDataFrame.csv')
# ...................................................................

def sc02():

    print "\nScenario 02: Grid-supplied Electricity Satisfies Cooling and Electric Loads and Buildings Use NG Onsite for Heating"
    
    # Calculate total cooling energy needed
    Output['NumHeatPumps_Cool'] = Output['SDL/CoolingMax']/Size_HeatPump_Cooling
    Output['NumHeatPumps_Cool'] = np.ceil(Output['NumHeatPumps_Cool'])
    Output['PLR_HeatPump_Cooling'] = np.where(Output['NumHeatPumps_Cool']<=0.0,0.0, Output['SDL/Cooling'] / (Output['NumHeatPumps_Cool']*Size_HeatPump_Cooling))
    Output['PLR_HeatPump_Cooling'] = np.where(Output['PLR_HeatPump_Cooling'] < MinPLR_HP_Cooling, MinPLR_HP_Cooling, Output['PLR_HeatPump_Cooling'])

    Output['COP_HeatPump_Cooling'] = ((c1_hp + c2_hp*Output['PLR_HeatPump_Cooling'] + c3_hp*Output['DB'] + 
    c4_hp*Output['PLR_HeatPump_Cooling']**2 + c5_hp*Output['PLR_HeatPump_Cooling']*Output['DB'] + 
    c6_hp*Output['DB']**2 + c7_hp*Output['PLR_HeatPump_Cooling']**3 + c8_hp*Output['PLR_HeatPump_Cooling']**2*Output['DB'] + 
    c9_hp*Output['PLR_HeatPump_Cooling']*Output['DB']**2 + c10_hp*Output['DB']**3)**-1)

    Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']>MaxCOP_Cooling, MaxCOP_Cooling,Output['COP_HeatPump_Cooling'])
    Output['COP_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling'] > MinCOP_Cooling, Output['COP_HeatPump_Cooling'], MinCOP_Cooling) # Added in v22 to set minimum COP to prevent negative values

    Output['Energy_HeatPump_Cooling'] = np.where(Output['COP_HeatPump_Cooling']<=0,0, Output['SDL/Cooling'] / Output['COP_HeatPump_Cooling'])
    Output['FanPowerSC01'] = Output['Energy_HeatPump_Cooling'] * FanPwrCoolRatio
    
    NonHVAC_Electricity = Output['Electricity']

    SC01_NonHVACEnergy = sum(NonHVAC_Electricity) * (1+Losses_Transmission) / Effic_PowerGen
    SC01_CoolingEnergy = sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) / Effic_PowerGen
    SC01_FanEnergy = sum(Output['FanPowerSC01']) * (1+Losses_Transmission) / Effic_PowerGen

    SC01_AvgCoolingCOP = sum(Output['SDL/Cooling']) / SC01_CoolingEnergy # This version of COP represents the true input energy needed to meet cooling demands

    Load_CitySC02 = sum(NonHVAC_Electricity + Output['Energy_HeatPump_Cooling'] + Output['FanPowerSC01'])
    Load_Grid_SC02 = Load_CitySC02 * (1 + Losses_Transmission)
    Energy_Grid_SC02 = Load_Grid_SC02/Effic_PowerGen

    Output['NumBoilers'] = Output['TotalHeatingMax']/Size_Boiler
    Output['NumBoilers'] = np.ceil(Output['NumBoilers'])

    # For every hour, calculate the PLR, Efficiency Boiler Modifier, and Energy Boiler
    Output['PLR_Boiler'] = np.where(Output['TotalHeating']<=0, 0, (Output['TotalHeating']/Output['NumBoilers'])/Size_Boiler)
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

    Cost_Electricity_SC02 = Load_Grid_SC02 * Cost_Electricity
    Cost_NG_SC02 = Energy_NaturalGas_SC02 * Price_NaturalGas
    Cost_SC02 = Cost_Electricity_SC02 + Cost_NG_SC02
    print "Annual cost of generating electricity and purchasing NG from grid (USD):" , Cost_SC02

    Emissions_Electricity_SC02 = Load_Grid_SC02 * Emissions_ElectricGeneration
    Emissions_NG_SC02 = Energy_NaturalGas_SC02 * Emissions_NG_Combustion_kWh
    Emissions_SC02 = Emissions_Electricity_SC02 + Emissions_NG_SC02
    print "Annual carbon emissions (Metric Tons):", Emissions_SC02 # updated text in v22

    Output['ElectricityConsumption'] = (NonHVAC_Electricity + Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01']) * (1 + Losses_Transmission)
    Output['NaturalGasConsumption'] = Output['TotalHeating'] /(Efficiency_Boiler_Nominal * Output['Efficiency_Boiler_Modifier'])

    Results = pd.DataFrame()
    # Results['Scenario02'] = SC02_NonHVACEnergy, SC02_HeatingEnergy, SC02_CoolingEnergy, Energy_SC02, SC02_AvgCoolingCOP, SC02_AvgHeatingCOP, Cost_SC02, Emissions_SC02
    Results['ElectricityConsumption'] = Output.groupby('Hour')['ElectricityConsumption'].sum()
    Results['NaturalGasConsumption'] = Output.groupby('Hour')['NaturalGasConsumption'].sum()

    # Export Data

    mkdir_p('C:/UMI/temp/DHSimulationResults')
    Output.to_csv('C:/UMI/temp/DHSimulationResults/SC02_Export_OutputDataFrame.csv')
    Results.to_csv('C:/UMI/temp/DHSimulationResults/SC02_Export_ResultsDataFrame.csv')
# ...................................................................

def sc03():

    print "\nScenario 03: Grid + District Heating and Cooling Plant"

    NonHVAC_Electricity = Output['Electricity']
    Load_City_SC03 = sum(NonHVAC_Electricity)
    Load_Grid_SC03 = Load_City_SC03 * (1 + Losses_Transmission)
    Energy_Grid_SC03 = Load_Grid_SC03 / Effic_PowerGen

    NumDistrictBoilers = np.ceil(max(PeakHeating) * (1 + Losses_Heat_Hydronic) /Size_DistrictBoiler) 

    DHC = pd.DataFrame()
    DHC['Hour'] = WeatherData.iloc[:,0].values
    DHC = DHC.set_index(['Hour'])
    DHC['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
    DHC['Drybulb'] = WeatherData.iloc[:,1].values

    DHC['PLR_DistrictBoiler'] = np.where(DHC['Load_HourlyHeating']<=0, 0, (DHC['Load_HourlyHeating']/NumDistrictBoilers)/Size_DistrictBoiler)
    DHC['PLR_DistrictBoiler'] = np.where(DHC['PLR_DistrictBoiler'] < MinPLR_Boiler, MinPLR_Boiler, DHC['PLR_DistrictBoiler']) # Added in v22
    DHC['Efficiency_DistrictBoiler_Modifier'] = c1 + c2 * DHC['PLR_DistrictBoiler'] + c3 * DHC['PLR_DistrictBoiler']**2 + c4 * DHC['PLR_DistrictBoiler']**3
    DHC['Energy_DistrictBoiler'] = DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier'])

    Energy_NaturalGas_SC03 = sum(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier']))

    DHC['COP_DistrictBoiler'] = np.where(DHC['Energy_DistrictBoiler']<=0,0,(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic)) / DHC['Energy_DistrictBoiler'])

    NumChillers = np.ceil(max(PeakCooling) * (1+ Losses_Heat_Hydronic) /Size_ElectricCentrifugalChiller_Nominal)
    DHC['Load_HourlyCooling'] = (Output.groupby('Hour')['SDL/Cooling'].sum()) * (1 + Losses_Heat_Hydronic)
    DHC['CoolingLoad/ChillerSize'] = DHC['Load_HourlyCooling']/Size_ElectricCentrifugalChiller_Nominal
    DHC['MinNumChillers'] = np.ceil(DHC['CoolingLoad/ChillerSize'])
    DHC['PLR'] = np.where(DHC['Load_HourlyCooling']<=0, 0, (DHC['Load_HourlyCooling']/DHC['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal) 
    DHC['PLR'] = np.where(DHC['PLR']<MinPLR_CentrifugalChiller, MinPLR_CentrifugalChiller, (DHC['Load_HourlyCooling']/DHC['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal)

    DHC['CWR_T'] = c1_CWR+c2_CWR*DHC['Drybulb']**2+c3_CWR*DHC['Drybulb'] * DHC['PLR']

    DHC['CapFunT'] = c1_ECC_CapFunT+c2_ECC_CapFunT*CHWS_T + c3_ECC_CapFunT*CHWS_T**2 + c4_ECC_CapFunT*DHC['CWR_T']+c5_ECC_CapFunT*DHC['CWR_T']**2 + c6_ECC_CapFunT*CHWS_T*DHC['CWR_T']
    DHC['EIRFunT'] = c1_ECC_EIRFunT+c2_ECC_EIRFunT*CHWS_T + c3_ECC_EIRFunT*CHWS_T**2 + c4_ECC_EIRFunT*DHC['CWR_T']+c5_ECC_EIRFunT*DHC['CWR_T']**2 + c6_ECC_EIRFunT*CHWS_T*DHC['CWR_T']
    DHC['EIRFunPLR'] = c1_ECC_EIRFPLR+c2_ECC_EIRFPLR*DHC['CWR_T'] + c3_ECC_EIRFPLR*DHC['CWR_T']**2 + c4_ECC_EIRFPLR*DHC['PLR']+c5_ECC_EIRFPLR*DHC['PLR']**2 + c6_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']+c7_ECC_EIRFPLR*DHC['CWR_T']**3+c8_ECC_EIRFPLR*DHC['PLR']**3+c9_ECC_EIRFPLR*DHC['CWR_T']**2*DHC['PLR']+c10_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']**2

    DHC['Energy_CentrifugalChiller'] = Power_CentrifugalChiller_Nominal * DHC['CapFunT'] * DHC['EIRFunT'] * DHC['EIRFunPLR'] * DHC['MinNumChillers']
    DHC['COP_CentrifugalChiller'] = np.where(DHC['Energy_CentrifugalChiller']<=0,0, DHC['Load_HourlyCooling']/DHC['Energy_CentrifugalChiller'])

    DHC['CoolingTowerPower'] = np.where(DHC['Load_HourlyCooling']<=0,0, Size_CT_Nominal * DHC['MinNumChillers'] * a1_ct * DHC['PLR']**3 + a2_ct * DHC['PLR']**2 -a3_ct * DHC['PLR'] + a4_ct)
    Energy_CoolingTowers_SC03 = sum(DHC['CoolingTowerPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    DHC['FanPower'] = DHC['Energy_CentrifugalChiller'] * FanPwrCoolRatio
    Energy_Fans_SC03 = sum(DHC['FanPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    DHC['PumpPower'] = DHC['Load_HourlyCooling'] * PumpCoolFraction + DHC['Load_HourlyHeating'] * (1+Losses_Heat_Hydronic) * PumpHeatFraction
    Energy_Pumps_SC03 = sum(DHC['PumpPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    Energy_CoolingElectricity_SC03 = sum(DHC['Energy_CentrifugalChiller']) * (1 + Losses_Transmission) / Effic_PowerGen
    Energy_SC03 = Energy_Grid_SC03 + Energy_NaturalGas_SC03 + Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03 + Energy_CoolingTowers_SC03

    SC03_AvgCoolingCOP = sum(Output['SDL/Cooling']) / ((Energy_CoolingElectricity_SC03 * (1 + Losses_Heat_Hydronic)) + Energy_CoolingTowers_SC03) # Includes chiller and CT energy in denom
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

    Cost_Electricity_SC03 = (Load_Grid_SC03 + (Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03) * Effic_PowerGen)* Cost_Electricity
    Cost_NG_SC03 = Energy_NaturalGas_SC03 * Price_NaturalGas
    Cost_SC03 = Cost_Electricity_SC03 + Cost_NG_SC03
    print "Annual cost of generating electricity for Non-HVAC loads and to run electric centrifugal chillers and purchasing NG from grid (USD):" , Cost_SC03

    Emissions_Electricity_SC03 = (Load_Grid_SC03 + (Energy_CoolingElectricity_SC03 + Energy_Fans_SC03 + Energy_Pumps_SC03) * Effic_PowerGen) * Emissions_ElectricGeneration
    Emissions_NG_SC03 = Energy_NaturalGas_SC03 * Emissions_NG_Combustion_kWh
    Emissions_SC03 = Emissions_Electricity_SC03 + Emissions_NG_SC03
    print "Annual carbon emissions (Metric Tons):", Emissions_SC03

    Results = pd.DataFrame()
    Results['Scenario03'] = Energy_Grid_SC03, Energy_NaturalGas_SC03, Energy_CoolingElectricity_SC03, Energy_SC03, SC03_AvgCoolingCOP, SC03_AvgHeatingCOP, Cost_SC03, Emissions_SC03
    DHC['ElectricityConsumption'] =  (NonHVAC_Electricity + DHC['Energy_CentrifugalChiller'] + DHC['FanPower']  + DHC['CoolingTowerPower'] + DHC['PumpPower']) * (1 + Losses_Transmission)
    DHC['NaturalGasConsumption'] = DHC['Energy_DistrictBoiler']

    # Export Data

    mkdir_p('C:/UMI/temp/DHSimulationResults')
    Output.to_csv('C:/UMI/temp/DHSimulationResults/SC03_Export_OutputDataFrame.csv')
    DHC.to_csv('C:/UMI/temp/DHSimulationResults/SC03_Export_DHCDataFrame.csv')
# ...................................................................

def sc04():

    print "\nScenario 04: CCHP Satisfies Electricity, Heating, and Cooling Demands"

    NumDistrictBoilers = np.ceil(max(PeakHeating) * (1 + Losses_Heat_Hydronic) /Size_DistrictBoiler) 

    DHC = pd.DataFrame()
    DHC['Hour'] = WeatherData.iloc[:,0].values
    DHC = DHC.set_index(['Hour'])
    DHC['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
    DHC['Drybulb'] = WeatherData.iloc[:,1].values

    DHC['PLR_DistrictBoiler'] = np.where(DHC['Load_HourlyHeating']<=0, 0, (DHC['Load_HourlyHeating']/NumDistrictBoilers)/Size_DistrictBoiler)
    DHC['PLR_DistrictBoiler'] = np.where(DHC['PLR_DistrictBoiler'] < MinPLR_Boiler, MinPLR_Boiler, DHC['PLR_DistrictBoiler']) # Added in v22
    DHC['Efficiency_DistrictBoiler_Modifier'] = c1 + c2 * DHC['PLR_DistrictBoiler'] + c3 * DHC['PLR_DistrictBoiler']**2 + c4 * DHC['PLR_DistrictBoiler']**3
    DHC['Energy_DistrictBoiler'] = DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier'])

    Energy_NaturalGas_SC03 = sum(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * DHC['Efficiency_DistrictBoiler_Modifier']))

    DHC['COP_DistrictBoiler'] = np.where(DHC['Energy_DistrictBoiler']<=0,0,(DHC['Load_HourlyHeating'] * (1 + Losses_Heat_Hydronic)) / DHC['Energy_DistrictBoiler'])

    NumChillers = np.ceil(max(PeakCooling) * (1+ Losses_Heat_Hydronic) /Size_ElectricCentrifugalChiller_Nominal)
    DHC['Load_HourlyCooling'] = (Output.groupby('Hour')['SDL/Cooling'].sum()) * (1 + Losses_Heat_Hydronic)
    DHC['CoolingLoad/ChillerSize'] = DHC['Load_HourlyCooling']/Size_ElectricCentrifugalChiller_Nominal
    DHC['MinNumChillers'] = np.ceil(DHC['CoolingLoad/ChillerSize'])
    DHC['PLR'] = np.where(DHC['Load_HourlyCooling']<=0, 0, (DHC['Load_HourlyCooling']/DHC['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal) 
    DHC['PLR'] = np.where(DHC['PLR']<MinPLR_CentrifugalChiller, MinPLR_CentrifugalChiller, (DHC['Load_HourlyCooling']/DHC['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal)

    DHC['CWR_T'] = c1_CWR+c2_CWR*DHC['Drybulb']**2+c3_CWR*DHC['Drybulb'] * DHC['PLR']

    DHC['CapFunT'] = c1_ECC_CapFunT+c2_ECC_CapFunT*CHWS_T + c3_ECC_CapFunT*CHWS_T**2 + c4_ECC_CapFunT*DHC['CWR_T']+c5_ECC_CapFunT*DHC['CWR_T']**2 + c6_ECC_CapFunT*CHWS_T*DHC['CWR_T']
    DHC['EIRFunT'] = c1_ECC_EIRFunT+c2_ECC_EIRFunT*CHWS_T + c3_ECC_EIRFunT*CHWS_T**2 + c4_ECC_EIRFunT*DHC['CWR_T']+c5_ECC_EIRFunT*DHC['CWR_T']**2 + c6_ECC_EIRFunT*CHWS_T*DHC['CWR_T']
    DHC['EIRFunPLR'] = c1_ECC_EIRFPLR+c2_ECC_EIRFPLR*DHC['CWR_T'] + c3_ECC_EIRFPLR*DHC['CWR_T']**2 + c4_ECC_EIRFPLR*DHC['PLR']+c5_ECC_EIRFPLR*DHC['PLR']**2 + c6_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']+c7_ECC_EIRFPLR*DHC['CWR_T']**3+c8_ECC_EIRFPLR*DHC['PLR']**3+c9_ECC_EIRFPLR*DHC['CWR_T']**2*DHC['PLR']+c10_ECC_EIRFPLR*DHC['CWR_T']*DHC['PLR']**2

    DHC['Energy_CentrifugalChiller'] = Power_CentrifugalChiller_Nominal * DHC['CapFunT'] * DHC['EIRFunT'] * DHC['EIRFunPLR'] * DHC['MinNumChillers']
    DHC['COP_CentrifugalChiller'] = np.where(DHC['Energy_CentrifugalChiller']<=0,0, DHC['Load_HourlyCooling']/DHC['Energy_CentrifugalChiller'])

    DHC['CoolingTowerPower'] = np.where(DHC['Load_HourlyCooling']<=0,0, Size_CT_Nominal * DHC['MinNumChillers'] * a1_ct * DHC['PLR']**3 + a2_ct * DHC['PLR']**2 -a3_ct * DHC['PLR'] + a4_ct)
    Energy_CoolingTowers_SC03 = sum(DHC['CoolingTowerPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    DHC['FanPower'] = DHC['Energy_CentrifugalChiller'] * FanPwrCoolRatio
    Energy_Fans_SC03 = sum(DHC['FanPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    DHC['PumpPower'] = DHC['Load_HourlyCooling'] * PumpCoolFraction + DHC['Load_HourlyHeating'] * (1+Losses_Heat_Hydronic) * PumpHeatFraction
    Energy_Pumps_SC03 = sum(DHC['PumpPower']) * (1 + Losses_Transmission) / Effic_PowerGen

    Energy_CoolingElectricity_SC03 = sum(DHC['Energy_CentrifugalChiller']) * (1 + Losses_Transmission) / Effic_PowerGen

    SC03_AvgCoolingCOP = sum(Output['SDL/Cooling']) / ((Energy_CoolingElectricity_SC03 * (1 + Losses_Heat_Hydronic)) + Energy_CoolingTowers_SC03) # Includes chiller and CT energy in denom
    SC03_AvgHeatingCOP = sum(Output['TotalHeating']) / Energy_NaturalGas_SC03


    CCHP = pd.DataFrame()
    CCHP['Hour'] = WeatherData.iloc[:,0].values
    CCHP = CCHP.set_index(['Hour'])
    CCHP['Load_HourlyElectricity'] = (Output.groupby('Hour')['Electricity'].sum())
    CCHP['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
    CCHP['Load_HourlyCooling'] = (Output.groupby('Hour')['SDL/Cooling'].sum()) * (1 + Losses_Heat_Hydronic)
    CCHP['Drybulb'] = WeatherData.iloc[:,1].values
    CCHP['CWR_T'] = DHC['CWR_T']
    CCHP['CHWS_T'] = CHWS_T

    Size_GasTurbine = max(CCHP['Load_HourlyElectricity']) #Rated electrical output of gas turbine based on the peak loads of the city

    CCHP['GT_PLR'] = CCHP['Load_HourlyElectricity'] / Size_GasTurbine
    CCHP['ModGT_PLR'] = np.where(CCHP['GT_PLR']<MinimumLoad_GasTurbine, MinimumLoad_GasTurbine, CCHP['Load_HourlyElectricity'] / Size_GasTurbine)

    CCHP['GT_Efficiency'] = (c1_GT*(CCHP['ModGT_PLR']*100)**2+c2_GT*(CCHP['ModGT_PLR']*100)+c3_GT)*Effic_CCHP_Electrical
    CCHP['Heat/Power'] = (1 - CCHP['GT_Efficiency'])*0.8 / CCHP['GT_Efficiency']
    CCHP['GT_InputFuelActual'] = CCHP['ModGT_PLR'] * Size_GasTurbine / CCHP['GT_Efficiency']
    Energy_GT = sum(CCHP['GT_InputFuelActual'])

    CCHP['GT_InputFuelOptimal'] = CCHP['Load_HourlyElectricity'] / CCHP['GT_Efficiency']
    CCHP['ExcessElectricity'] = (CCHP['GT_InputFuelActual'] - CCHP['GT_InputFuelOptimal']) * CCHP['GT_Efficiency']
    ExcessElecValue = sum(CCHP['ExcessElectricity']) * Cost_Electricity # added in v22

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

    # 00 Initialization of COP and while loop set up for absorption chiller ---
    CCHP['COP_AbsChiller'] = CoP_Chiller_Absorption * 0.75 #Initial guess for COP
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

    # ------- Supplemental Cooling by an Electric Centrifugal Chiller Begin ------- #
    NumElectricChillers = np.ceil(max(CCHP['CoolingSupp'])/Size_ElectricCentrifugalChiller_Nominal)

    CCHP['CoolingLoad/ChillerSize'] = CCHP['CoolingSupp']/Size_ElectricCentrifugalChiller_Nominal
    CCHP['MinNumChillers'] = np.ceil(CCHP['CoolingLoad/ChillerSize']) 
    CCHP['PLR_ElectricChiller'] = np.where(CCHP['CoolingSupp']<=0, 0, (CCHP['CoolingSupp']/ CCHP['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal)
    CCHP['PLR_ElectricChiller'] = np.where(CCHP['PLR_ElectricChiller'] < MinPLR_CentrifugalChiller, MinPLR_CentrifugalChiller, np.where(CCHP['CoolingSupp']<=0, 0, (CCHP['CoolingSupp']/ CCHP['MinNumChillers'])/Size_ElectricCentrifugalChiller_Nominal))

    CCHP['CapFunT'] = c1_ECC_CapFunT+c2_ECC_CapFunT*CHWS_T + c3_ECC_CapFunT*CHWS_T**2 + c4_ECC_CapFunT*CCHP['CWR_T']+c5_ECC_CapFunT*CCHP['CWR_T']**2 + c6_ECC_CapFunT*CHWS_T*CCHP['CWR_T']
    CCHP['EIRFunT'] = c1_ECC_EIRFunT+c2_ECC_EIRFunT*CHWS_T + c3_ECC_EIRFunT*CHWS_T**2 + c4_ECC_EIRFunT*CCHP['CWR_T']+c5_ECC_EIRFunT*CCHP['CWR_T']**2 + c6_ECC_EIRFunT*CHWS_T*CCHP['CWR_T']
    CCHP['EIRFunPLR'] = c1_ECC_EIRFPLR+c2_ECC_EIRFPLR*CCHP['CWR_T'] + c3_ECC_EIRFPLR*CCHP['CWR_T']**2 + c4_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']+c5_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']**2 + c6_ECC_EIRFPLR*CCHP['CWR_T']*CCHP['PLR_ElectricChiller']+c7_ECC_EIRFPLR*CCHP['CWR_T']**3+c8_ECC_EIRFPLR*CCHP['PLR_ElectricChiller']**3+c9_ECC_EIRFPLR*CCHP['CWR_T']**2*CCHP['PLR_ElectricChiller']+c10_ECC_EIRFPLR*CCHP['CWR_T']*CCHP['PLR_ElectricChiller']**2

    CCHP['Energy_CentrifugalChiller'] = Power_CentrifugalChiller_Nominal * CCHP['CapFunT'] * CCHP['EIRFunT'] * CCHP['EIRFunPLR'] * CCHP['MinNumChillers']
    CCHP['FanPower'] = DHC['FanPower']
    CCHP['PumpPower'] = DHC['PumpPower']

    CCHP['COP_CentrifugalChiller'] = np.where(CCHP['Energy_CentrifugalChiller']<=0,0, CCHP['CoolingSupp'] / CCHP['Energy_CentrifugalChiller'])
    CCHP['CoolingTowerPower'] = Size_CT_Nominal * CCHP['MinNumChillers'] * a1_ct * CCHP['PLR_ElectricChiller']**3 + a2_ct * CCHP['PLR_ElectricChiller']**2 - a3_ct * CCHP['PLR_ElectricChiller']* + a4_ct

    Energy_CoolingSuppElectricity_SC04 = sum(CCHP['Energy_CentrifugalChiller']) * (1 + Losses_Transmission) / Effic_PowerGen

    Energy_Fans_SC04 = Energy_Fans_SC03
    Energy_Pumps_SC04 = Energy_Pumps_SC03
    Energy_CoolingTowers_SC04 = Energy_CoolingTowers_SC03

    Energy_SC04 = Energy_GT + Energy_CCHPBoiler + Energy_CoolingSuppElectricity_SC04 + Energy_Fans_SC04 + Energy_Pumps_SC04 + Energy_CoolingTowers_SC04

    InputEnergyAbsChiller = (sum(Output['SDL/Cooling']) - sum(CCHP['CoolingSupp']))/CoP_Chiller_Absorption
    SC04_AvgCoolingCOP = sum(Output['SDL/Cooling']) / (InputEnergyAbsChiller + Energy_CoolingSuppElectricity_SC04 + Energy_CoolingTowers_SC04) #Excludes pumping losses
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

    Cost_Electricity_SC04 = (Energy_CoolingSuppElectricity_SC04 * Effic_PowerGen) * Cost_Electricity
    Cost_NG_SC04 = (Energy_GT + Energy_CCHPBoiler) * Price_NaturalGas
    Cost_SC04 = Cost_Electricity_SC04 + Cost_NG_SC04

    print "Annual cost (USD):" , Cost_SC04
    print "Surplus electricity value (USD):" , ExcessElecValue # added v22
    print "Updated annual cost (USD):" , Cost_SC04 - ExcessElecValue # added v22

    Emissions_Electricity_SC04 = (Energy_CoolingSuppElectricity_SC04 * Effic_PowerGen) * Emissions_ElectricGeneration
    Emissions_NG_SC04 = (Energy_GT + Energy_CCHPBoiler) * Emissions_NG_Combustion_kWh
    Emissions_SC04 = Emissions_Electricity_SC04 + Emissions_NG_SC04
    print "Annual carbon emissions (Metric Tons):", Emissions_SC04

    # Results = pd.DataFrame()
    # Results['Scenario04'] = Energy_Grid_SC03, Energy_NaturalGas_SC03, Energy_CoolingElectricity_SC03, Energy_SC03, SC03_AvgCoolingCOP, SC03_AvgHeatingCOP, Cost_SC03, Emissions_SC03

    CCHP['ElectricityConsumption'] =  (CCHP['Energy_CentrifugalChiller'] + CCHP['FanPower'] + CCHP['CoolingTowerPower'] + CCHP['PumpPower']) * (1 + Losses_Transmission)
    CCHP['NaturalGasConsumption'] = CCHP['HeatSupplementary'] * (1 + Losses_Heat_Hydronic) /(Efficiency_Boiler_Nominal * CCHP['Efficiency_CCHPBoiler_Modifier']) + CCHP['GT_InputFuelActual']

    # ...................................................................
    # Export Data

    
    mkdir_p('C:/UMI/temp/DHSimulationResults')
    # Output.to_csv('C:/UMI/temp/DHSimulationResults/SC04_Export_OutputDataFrame.csv')
    DHC.to_csv('C:/UMI/temp/DHSimulationResults/SC04_Export_DHCDataFrame.csv')
    CCHP.to_csv('C:/UMI/temp/DHSimulationResults/SC04_Export_CCHPDataFrame.csv')
# ...................................................................

# map the inputs to the function blocks
options = {1 : sc01,
           2 : sc02,
           3 : sc03,
           4 : sc04,
}

# Call function
num = int(sys.argv[1])
options[num]()
