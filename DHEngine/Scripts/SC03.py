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
Losses_Heat_Hydronic = float(sys.argv[5]) #Heat transfer losses from hydronic distribution systemission losses https://www.eia.gov/tools/faqs/faq.php?id=105&t=3

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

print "\nScenario 03: Grid + District Heating and Cooling Plant"

NonHVAC_Electricity = Output['Electricity']
Load_City_SC03 = sum(NonHVAC_Electricity)
Load_Grid_SC03 = Load_City_SC03 * (1 + Losses_Transmission)
Energy_Grid_SC03 = Load_Grid_SC03 / Effic_PowerGen

NumDistrictBoilers = np.ceil(max(PeakHeating) * (1 + Losses_Heat_Hydronic) /Size_DistrictBoiler) 

DHC = pd.DataFrame()

DHC['Load_HourlyHeating'] = Output.groupby('Hour')['TotalHeating'].sum()
DHC['Drybulb'] = Output['DB']

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

# ...................................................................
# Export Data

Output.to_csv('C:/UMI/temp/DHSimulationResults/SC03_Export_OutputDataFrame.csv')
DHC.to_csv('C:/UMI/temp/DHSimulationResults/SC03_Export_DHCDataFrame.csv')