import pandas as pd 
import numpy as np
import sys

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

# ...................................................................

# Internal Parameters
kWhPerTherm = 29.3 #kwh/therm
Emissions_NG_Combustion_therm = 0.005302 #Metric ton CO2 per therm of NG
Emissions_NG_Combustion_kWh = Emissions_NG_Combustion_therm / kWhPerTherm
FanPwrCoolRatio = 0 # 34.0/27.0 removed because assuming fan energy ~constant for all cases

# Empirical heat pump model from Purdue for heating mode
Size_HeatPump_Heating = 3.516825 #Nominal rated capacity of the heat pump in heating mode [kW]
Power_Max_Heating = 2.164 #Max work by compressor in kW
MinPLR_HP_Heating = 0.28
MinCOP_Heating = 0.0

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
print "Scenario 01: Grid Supplied Electricity Satisfies All Heating, Cooling, and Electric loads"

# Calculate total heating energy needed

Output['NumHeatPumps_Heat'] = Output['TotalHeatingMax']/Size_HeatPump_Heating 
Output['NumHeatPumps_Heat'] = np.ceil(Output['NumHeatPumps_Heat'])
Output['PLR_HeatPump_Heating'] = np.where(Output['TotalHeating']<=0, 0, (Output['TotalHeating']/Output['NumHeatPumps_Heat'])/Size_HeatPump_Heating)
Output['PLR_HeatPump_Heating'] = np.where(Output['PLR_HeatPump_Heating']<=MinPLR_HP_Heating, MinPLR_HP_Heating, (Output['TotalHeating']/Output['NumHeatPumps_Heat'])/Size_HeatPump_Heating)
Output['HP_Heat_Modifier'] = a1 + a2 * Output['PLR_HeatPump_Heating'] + a3 * Output['PLR_HeatPump_Heating']**2 + a4 * Output['PLR_HeatPump_Heating'] + a5 * Output['PLR_HeatPump_Heating']**3 + a6 * Output['PLR_HeatPump_Heating'] + a7 * Output['PLR_HeatPump_Heating']
Output['Energy_HeatPump_Heating'] = Output['HP_Heat_Modifier'] * Power_Max_Heating * Output['NumHeatPumps_Heat']
Output['COP_HeatPump_Heating'] = np.where(Output['Energy_HeatPump_Heating'] < MinCOP_Heating,MinCOP_Heating, Output['TotalHeating'] / Output['Energy_HeatPump_Heating'])
Output['COP_HeatPump_Heating'] = np.where(Output['COP_HeatPump_Heating'] >MaxCOP_Heating, MaxCOP_Heating, Output['COP_HeatPump_Heating'])

# ...................................................................

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
SC01_CoolingEnergy = sum(Output['Energy_HeatPump_Cooling']) * (1+Losses_Transmission) / Effic_PowerGen
SC01_FanEnergy = sum(Output['FanPowerSC01']) * (1+Losses_Transmission) / Effic_PowerGen

SC01_AvgCoolingCOP = sum(Output['SDL/Cooling']) / SC01_CoolingEnergy # This version of COP represents the true input energy needed to meet cooling demands
SC01_AvgHeatingCOP = sum(Output['TotalHeating']) / SC01_HeatingEnergy 

print "Annual input energy for non-HVAC electricity loads:", SC01_NonHVACEnergy
print "Annual input energy for heating:", SC01_HeatingEnergy
print "Annual input energy for cooling:", SC01_CoolingEnergy
print "Annual input energy required (kWh):", Energy_SC01
print "Average Cooling COP:", SC01_AvgCoolingCOP
print "Average Heating COP:", SC01_AvgHeatingCOP

# Cost to Generate Electricity
Cost_SC01 = Load_Grid_SC01 * Cost_Electricity
print "Annual cost of generating electricity for grid (USD):" , Cost_SC01

# Calculate CO2 Emissions Associated to Using Electricity from the Grid to Satisfy Loads
Emissions_SC01 = Load_Grid_SC01 * Emissions_ElectricGeneration
print "Annual carbon emissions (Metric Tons):", Emissions_SC01

Output['ElectricityConsumption'] = (NonHVAC_Electricity + Output['Energy_HeatPump_Heating'] + Output['Energy_HeatPump_Cooling']+Output['FanPowerSC01']) * (1 + Losses_Transmission)
Output['NaturalGasConsumption'] = 0.0

Results = pd.DataFrame()
Results['ElectricityConsumption'] = Output.groupby('Hour')['ElectricityConsumption'].sum()
Results['NaturalGasConsumption'] = Output.groupby('Hour')['NaturalGasConsumption'].sum()
# Results['Scenario01'] = SC01_NonHVACEnergy, SC01_HeatingEnergy, SC01_CoolingEnergy, Energy_SC01, SC01_AvgCoolingCOP, SC01_AvgHeatingCOP, Cost_SC01, Emissions_SC01

# ...................................................................
# Export Data

Output.to_csv('C:/UMI/temp/DHSimulationResults/SC01_Export_OutputDataFrame.csv')
Results.to_csv('C:/UMI/temp/DHSimulationResults/SC01_Export_ResultsDataFrame.csv')