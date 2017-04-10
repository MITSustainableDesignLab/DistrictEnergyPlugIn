import pandas as pd 
import numpy as np
import sys
import pdb # use pdb.set_trace()

# > > > Load Input Data [DYNAMIC INPUTS] < < <
LoadData = pd.read_csv('C:/UMI/temp/Loads.csv')
Output = LoadData #Reformatted Kuwait output to bypass the reformat section
WeatherData = pd.read_csv('C:/UMI/temp/DryBulbData.csv')

# > > > User-specified Parameters [DYNAMIC INPUTS] < < < 
Cost_Electricity = float(sys.argv[1]) #Generation cost per kWh Source: https://www.oxfordenergy.org/wpcms/wp-content/uploads/2014/04/MEP-9.pdf
Price_NaturalGas = float(sys.argv[2]) #Dollars per kWh
Emissions_ElectricGeneration = float(sys.argv[3]) #Metric ton CO2 per kWh produced
Effic_PowerGen = float(sys.argv[6]) #Average thermal efficiency of electrical generation in Kuwait
Losses_Transmission = float(sys.argv[4]) #Electrical transmission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3
Losses_Heat_Hydronic = float(sys.argv[5]) #Heat transfer losses from hydronic distribution systemission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3

# ...................................................................

# Internal Parameters
CW_Delta = 11 # Difference beween CWS and CWR in degrees celsius
kWhPerTherm = 29.3 #kwh/therm
Emissions_NG_Combustion_therm = 0.005302 #Metric ton CO2 per therm of NG
Emissions_NG_Combustion_kWh = Emissions_NG_Combustion_therm / kWhPerTherm
FanPwrCoolRatio = 0 # 34.0/27.0 removed because assuming fan energy ~constant for all cases

# New style, low-temperature Boiler
Size_Boiler = 15 #Nominal rated capacity of the boiler [kW] (size not specifed by EnergyPlus)
Efficiency_Boiler_Nominal = 0.8
MinPLR_Boiler = 0.10

# Cubic coeffcients 
c1 = 0.83888652
c2 = 0.132579019
c3 = -0.17028503
c4 = 0.047468326

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

# ...................................................................

# Update Output dataframe
Output['TotalHeating'] = Output['SDL/Heating'] + Output['SDL/Domestic Hot Water']
Output['Electricity'] = Output['SDL/Equipment'] + Output['SDL/Lighting']
Output = Output.join(WeatherData['DB'], on='Hour')

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

# ...................................................................
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

# ...................................................................

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
Results['ElectricityConsumption'] = Output.groupby('Hour')['ElectricityConsumption'].sum()
Results['NaturalGasConsumption'] = Output.groupby('Hour')['NaturalGasConsumption'].sum()
# Results['Scenario02'] = SC02_NonHVACEnergy, SC02_HeatingEnergy, SC02_CoolingEnergy, Energy_SC02, SC02_AvgCoolingCOP, SC02_AvgHeatingCOP, Cost_SC02, Emissions_SC02

# ...................................................................
# Export Data

Output.to_csv('C:/UMI/temp/DHSimulationResults/SC02_Export_OutputDataFrame.csv')
Results.to_csv('C:/UMI/temp/DHSimulationResults/SC02_Export_ResultsDataFrame.csv')


