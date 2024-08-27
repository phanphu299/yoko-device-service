delete FROM uoms where name in (
    'count',
    'coulomb',
    'Ampere hour',
    'kilogram per second',
    'long ton per day',
    'million pound per day',
    'short ton per day',
    'thousand pound per day',
    'gram per second',
    'pound per second',
    'tonne per day',
    'gram',
    'milligram',
    'pound',
    'tonne',
    'long ton',
    'million pound',
    'ounce',
    'short ton',
    'thousand pound',
    'ton',
    'kilogram',
    'second',
    'hour',
    'month',
    'week',
    'year',
    'day',
    'minute',
    'dyne',
    'kilogram-force',
    'pound-force',
    'newton',
    'million British thermal unit per day',
    'million British thermal unit per hour',
    'watt',
    'gigawatt',
    'megawatt',
    'British thermal unit per hour',
    'calorie per second',
    'horsepower',
    'joule per second',
    'kilowatt',
    'megajoule per hour',
    'million calorie per hour',
    'mole per second',
    'gram mole per second',
    'kilogram mole per second',
    'pound mole per second',
    'meter',
    'centimeter',
    'inch',
    'International nautical mile',
    'kilometer',
    'millimeter',
    'foot',
    'mile',
    'sixteenth of an inch',
    'yard',
    'candela',
    'meter per second',
    'centimeter per second',
    'foot per second',
    'International nautical mile per hour',
    'kilometer per hour',
    'mile per hour',
    'revolution per minute',
    'radian per second',
    'British thermal unit per degree Rankine',
    'British thermal unit per degree Fahrenheit',
    'kilojoule per kelvin',
    'joule per kelvin',
    'cubic meter per second',
    'barrel per day',
    'cubic centimeter per second',
    'cubic foot per second',
    'cubic meter per hour',
    'Imperial gallon per minute',
    'liter per second',
    'US gallon per minute',
    'pascal',
    'atmosphere',
    'bar',
    'inches of mercury',
    'kilogram-force per square centimeter',
    'kilogram-force per square meter',
    'kilopascal',
    'millimeter of mercury',
    'newton per square meter',
    'pound-force per square inch',
    'pound-force per square inch (customary)',
    'torr',
    'square meter',
    'hectare',
    'square centimeter',
    'square inch',
    'square kilometer',
    'square millimeter',
    'square foot',
    'acre',
    'square mile',
    'square yard',
    'degree Celsius',
    'degree Rankine',
    'degree Fahrenheit',
    'kelvin',
    'milliampere',
    'ampere',
    'joule per kilogram',
    'joule per gram',
    'British thermal unit per pound',
    'kilocalorie per kilogram',
    'kilojoule per kilogram',
    'kilojoule per pound',
    'hertz',
    'British thermal unit per pound degree Rankine',
    'British thermal unit per pound degree Fahrenheit',
    'joule per gram kelvin',
    'kilojoule per kilogram kelvin',
    'joule per kilogram kelvin',
    'mole',
    'gram mole',
    'kilogram mole',
    'pound mole',
    'parts per billion',
    'parts per million',
    'percent',
    'kilogram per mole',
    'gram per gram mole',
    'pound per pound mole',
    'kilogram per kilogram mole',
    'volt',
    'kilovolt',
    'megavolt',
    'gigawatt hour',
    'megawatt hour',
    'watt hour',
    'joule',
    'British thermal unit',
    'calorie',
    'gigajoule',
    'kilojoule',
    'kilowatt hour',
    'megajoule',
    'watt second',
    'kilocalorie',
    'million calorie',
    'million British thermal unit',
    'million imperial gallon',
    'thousand imperial gallon',
    'barrel',
    'Imperial gallon',
    'million US gallon',
    'thousand US gallon',
    'cubic centimeter',
    'cubic foot',
    'kiloliter',
    'liter',
    'megaliter',
    'milliliter',
    'thousand cubic meter',
    'US gallon',
    'million barrel',
    'thousand barrel',
    'acre foot',
    'cubic meter',
    'ohm',
    'kilogram per cubic meter',
    'gram per liter',
    'kilogram per liter',
    'pound per barrel',
    'pound per cubic foot',
    'pound per US gallon',
    'tonne per cubic meter',
    'radian',
    'degree',
    'revolution',
    'pascal second',
    'poise',
    'cubic foot per pound',
    'cubic centimeter per gram',
    'cubic meter per kilogram',
    'delta kelvin',
    'delta degree Fahrenheit',
    'delta degree Rankine',
    'delta degree Celsius'
);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('count',1,0,'QUANTITY','count',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('coulomb',1,0,'ELECTRIC_CHARGE','C',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('Ampere hour',3600,0,'ELECTRIC_CHARGE','Ah',3600,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram per second',1,0,'MASS_FLOW_RATE','kg/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('long ton per day',0.0259259259259259,0,'MASS_FLOW_RATE','lton/d',0.0117598021851852,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million pound per day',11.5740740740741,0,'MASS_FLOW_RATE','MMlb/d',5.24991168981483,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('short ton per day',0.0231481481481481,0,'MASS_FLOW_RATE','ston/d',0.0104998233796296,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand pound per day',0.0115740740740741,0,'MASS_FLOW_RATE','klb/d',0.00524991168981483,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram per second',0.001,0,'MASS_FLOW_RATE','g/s',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound per second',0.45359237,0,'MASS_FLOW_RATE','lb/s',0.45359237,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('tonne per day',0.0115740740740741,0,'MASS_FLOW_RATE','t/d',0.0115740740740741,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram',0.001,0,'MASS','g',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('milligram',0.000001,0,'MASS','mg',0.000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound',0.45359237,0,'MASS','lb',0.45359237,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('tonne',1000,0,'MASS','t',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('long ton',2240,0,'MASS','lton',1016.0469088,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million pound',1000000,0,'MASS','MM lb',453592.37,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('ounce',0.0625,0,'MASS','oz',0.028349523125,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('short ton',2000,0,'MASS','ston',907.18474,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand pound',1000,0,'MASS','klb',453.59237,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('ton',2000,0,'MASS','ton',907.18474,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram',1,0,'MASS','kg',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('second',1,0,'TIME','s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('hour',60,0,'TIME','h',3600,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('month',30.4166666666667,0,'TIME','month',2628000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('week',7,0,'TIME','week',604800,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('year',365,0,'TIME','yr',31536000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('day',24,0,'TIME','d',86400,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('minute',60,0,'TIME','min',60,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('dyne',0.00001,0,'FORCE','dyne',0.00001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram-force',9.80665,0,'FORCE','kgf',9.80665,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound-force',4.44822161526,0,'FORCE','lbf',4.44822161526,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('newton',1,0,'FORCE','N',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million British thermal unit per day',41666.6666666667,0,'POWER','MM Btu/d',12211.2945905092,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million British thermal unit per hour',1000000,0,'POWER','MM Btu/h',293071.07017222,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('watt',1,0,'POWER','W',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gigawatt',1000000000,0,'POWER','GW',1000000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megawatt',1000000,0,'POWER','MW',1000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per hour',0.29307107017222,0,'POWER','Btu/h',0.29307107017222,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('calorie per second',4.1868,0,'POWER','cal/s',4.1868,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('horsepower',745.699871582,0,'POWER','hp',745.699871582,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per second',1,0,'POWER','J/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilowatt',1000,0,'POWER','kW',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megajoule per hour',277.777777777778,0,'POWER','MJ/h',277.777777777778,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million calorie per hour',1163,0,'POWER','MMcal/h',1163,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('mole per second',1,0,'MOLAR_FLOW_RATE','mol/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram mole per second',1,0,'MOLAR_FLOW_RATE','gmol/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram mole per second',1000,0,'MOLAR_FLOW_RATE','kmol/s',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound mole per second',453.59237,0,'MOLAR_FLOW_RATE','lbmol/s',453.59237,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('meter',1,0,'LENGTH','m',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('centimeter',0.01,0,'LENGTH','cm',0.01,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('inch',0.0254,0,'LENGTH','in',0.0254,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('International nautical mile',1852,0,'LENGTH','nmi',1852,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilometer',1000,0,'LENGTH','km',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('millimeter',0.001,0,'LENGTH','mm',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('foot',12,0,'LENGTH','ft',0.3048,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('mile',63360,0,'LENGTH','mi',1609.344,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('sixteenth of an inch',0.0625,0,'LENGTH','sxi',0.0015875,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('yard',36,0,'LENGTH','yd',0.9144,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('candela',1,0,'LUMINOUS_INTENSITY','cd',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('meter per second',1,0,'SPEED','m/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('centimeter per second',0.01,0,'SPEED','cm/s',0.01,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('foot per second',0.3048,0,'SPEED','ft/s',0.3048,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('International nautical mile per hour',0.514444444444444,0,'SPEED','nmi/h',0.514444444444444,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilometer per hour',0.277777777777778,0,'SPEED','km/h',0.277777777777778,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('mile per hour',0.44704,0,'SPEED','mi/h',0.44704,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('revolution per minute',0.10471975511966,0,'ANGULARVELOCITY','rpm',0.10471975511966,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('radian per second',1,0,'ANGULARVELOCITY','rad/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per degree Rankine',1899.100534716,0,'ENTROPY_HEAT_CAPACITY','Btu/°R',1899.100534716,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per degree Fahrenheit',1899.100534716,0,'ENTROPY_HEAT_CAPACITY','Btu/°F',1899.100534716,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilojoule per kelvin',1000,0,'ENTROPY_HEAT_CAPACITY','kJ/K',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per kelvin',1,0,'ENTROPY_HEAT_CAPACITY','J/K',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic meter per second',1,0,'VOLUMEFLOW RATE','m3/s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('barrel per day',1.84013072833333E-06,0,'VOLUMEFLOW RATE','bbl/d',1.84013072833333E-06,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic centimeter per second',0.000001,0,'VOLUMEFLOW RATE','cm3/s',0.000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic foot per second',0.028316846592,0,'VOLUMEFLOW RATE','ft3/s',0.028316846592,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic meter per hour',0.000277777777777778,0,'VOLUMEFLOW RATE','m3/h',0.000277777777777778,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('Imperial gallon per minute',7.57681666666663E-05,0,'VOLUMEFLOW RATE','Imp gal/min',7.57681666666663E-05,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('liter per second',0.001,0,'VOLUMEFLOW RATE','L/s',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('US gallon per minute',0.0000630901964,0,'VOLUMEFLOW RATE','US gal/min',0.0000630901964,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pascal',1,0,'PRESSURE','Pa',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('atmosphere',101325,0,'PRESSURE','atm',101325,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('bar',100000,0,'PRESSURE','bar',100000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('inches of mercury',3386.38815789,0,'PRESSURE','inHg',3386.38815789,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram-force per square centimeter',98066.5,0,'PRESSURE','kgf/cm2',98066.5,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram-force per square meter',9.80665,0,'PRESSURE','kgf/m2',9.80665,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilopascal',1000,0,'PRESSURE','kPa',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('millimeter of mercury',133.322368421,0,'PRESSURE','mmHg',133.322368421,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('newton per square meter',1,0,'PRESSURE','N/m2',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound-force per square inch',6894.75729316836,0,'PRESSURE','psi',6894.75729316836,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound-force per square inch (customary)',6894.75729316836,0,'PRESSURE','psia',6894.75729316836,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('torr',133.322368421,0,'PRESSURE','torr',133.322368421,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square meter',1,0,'AREA','m2',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('hectare',10000,0,'AREA','ha',10000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square centimeter',0.0001,0,'AREA','cm2',0.0001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square inch',0.00064516,0,'AREA','in2',0.00064516,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square kilometer',1000000,0,'AREA','km2',1000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square millimeter',0.000001,0,'AREA','mm2',0.000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square foot',144,0,'AREA','ft2',0.09290304,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('acre',43560,0,'AREA','acre',4046.8564224,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square mile',27878400,0,'AREA','mi2',2589988.110336,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('square yard',9,0,'AREA','yd2',0.83612736,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('degree Celsius',1,273.15,'TEMPERATURE','°C',1,273.15);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('degree Rankine',1,-459.67,'TEMPERATURE','°R',0.555555555555556,-2.55795384873636E-13);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('degree Fahrenheit',0.555555555555556,-17.7777777777778,'TEMPERATURE','°F',0.555555555555556,255.372222222222);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kelvin',1,0,'TEMPERATURE','K',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('milliampere',0.001,0,'ELECTRIC_CURRENT','mA',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('ampere',1,0,'ELECTRIC_CURRENT','A',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per kilogram',1,0,'SPECIFIC_ENERGY','J/kg',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per gram',1000,0,'SPECIFIC_ENERGY','J/g',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per pound',2326,0,'SPECIFIC_ENERGY','Btu/lb',2326,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilocalorie per kilogram',4186.8,0,'SPECIFIC_ENERGY','kcal/kg',4186.8,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilojoule per kilogram',1000,0,'SPECIFIC_ENERGY','kJ/kg',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilojoule per pound',2204.62262184878,0,'SPECIFIC_ENERGY','kJ/lb',2204.62262184878,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('hertz',1,0,'FREQUENCY','Hz',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per pound degree Rankine',4186.8,0,'SPECIFIC_ENTROPY_SPECIFIC_HEAT_CAPACITY','Btu/(lb °R)',4186.8,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit per pound degree Fahrenheit',4186.8,0,'SPECIFIC_ENTROPY_SPECIFIC_HEAT_CAPACITY','Btu/(lb °F)',4186.8,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per gram kelvin',1000,0,'SPECIFIC_ENTROPY_SPECIFIC_HEAT_CAPACITY','J/(g K)',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilojoule per kilogram kelvin',1000,0,'SPECIFIC_ENTROPY_SPECIFIC_HEAT_CAPACITY','kJ/(kg K)',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule per kilogram kelvin',1,0,'SPECIFIC_ENTROPY_SPECIFIC_HEAT_CAPACITY','J/(kg K)',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('mole',1,0,'MOLES','mol',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram mole',1,0,'MOLES','gmol',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram mole',1000,0,'MOLES','kmol',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound mole',453.59237,0,'MOLES','lbmol',453.59237,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('parts per billion',0.0000001,0,'RATIO','ppb',0.0000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('parts per million',0.0001,0,'RATIO','ppm',0.0001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('percent',1,0,'RATIO','%',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram per mole',1,0,'MOLECULAR_WEIGHT','kg/mol',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram per gram mole',1,0,'MOLECULAR_WEIGHT','g/gmol',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound per pound mole',1,0,'MOLECULAR_WEIGHT','lb/lbmol',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram per kilogram mole',0.001,0,'MOLECULAR_WEIGHT','kg/kmol',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('volt',1,0,'ELECTRIC_POTENTIAL','V',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilovolt',1000,0,'ELECTRIC_POTENTIAL','kV',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megavolt',1000000,0,'ELECTRIC_POTENTIAL','MV',1000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gigawatt hour',3600000000000,0,'ENERGY','GWh',3600000000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megawatt hour',3600000000,0,'ENERGY','MWh',3600000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('watt hour',3600,0,'ENERGY','Wh',3600,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('joule',1,0,'ENERGY','J',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('British thermal unit',1055.05585262,0,'ENERGY','Btu',1055.05585262,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('calorie',4.1868,0,'ENERGY','cal',4.1868,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gigajoule',1000000000,0,'ENERGY','GJ',1000000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilojoule',1000,0,'ENERGY','kJ',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilowatt hour',3600000,0,'ENERGY','kWh',3600000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megajoule',1000000,0,'ENERGY','MJ',1000000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('watt second',1,0,'ENERGY','Ws',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilocalorie',1000,0,'ENERGY','kcal',4186.8,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million calorie',1000000,0,'ENERGY','MMcal',4186800,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million British thermal unit',1000000,0,'ENERGY','MM Btu',1055055852.62,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million imperial gallon',1000000,0,'VOLUME','Imp Mgal',4546.08999999998,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand imperial gallon',1000,0,'VOLUME','Imp kgal',4.54608999999998,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('barrel',42,0,'VOLUME','bbl',0.158987294928,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('Imperial gallon',1.20094992550485,0,'VOLUME','Imp gal',0.00454608999999998,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million US gallon',1000000,0,'VOLUME','US Mgal',3785.411784,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand US gallon',1000,0,'VOLUME','US kgal',3.785411784,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic centimeter',0.000001,0,'VOLUME','cm3',0.000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic foot',0.028316846592,0,'VOLUME','ft3',0.028316846592,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kiloliter',1,0,'VOLUME','kL',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('liter',0.001,0,'VOLUME','L',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('megaliter',1000,0,'VOLUME','M L',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('milliliter',0.000001,0,'VOLUME','mL',0.000001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand cubic meter',1000,0,'VOLUME','k m3',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('US gallon',0.003785411784,0,'VOLUME','US gal',0.003785411784,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('million barrel',1000000,0,'VOLUME','MMbbl',158987.294928,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('thousand barrel',1000,0,'VOLUME','kbbl',158.987294928,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('acre foot',43560,0,'VOLUME','acre ft',1233.48183754752,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic meter',1,0,'VOLUME','m3',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('ohm',1,0,'ELECTRIC_RESISTANCE','Ω',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram per cubic meter',1,0,'DENSITY','kg/m3',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('gram per liter',1,0,'DENSITY','g/L',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('kilogram per liter',1000,0,'DENSITY','kg/L',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound per barrel',2.85301017421182,0,'DENSITY','lb/bbl',2.85301017421182,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound per cubic foot',16.0184633739601,0,'DENSITY','lb/ft3',16.0184633739601,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pound per US gallon',119.826427316897,0,'DENSITY','lb/US gal',119.826427316897,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('tonne per cubic meter',1000,0,'DENSITY','t/m3',1000,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('radian',1,0,'PLANEANGLE','rad',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('degree',0.0174532925199433,0,'PLANEANGLE','°',0.0174532925199433,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('revolution',6.28318530717959,0,'PLANEANGLE','r',6.28318530717959,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('pascal second',1,0,'DYNAMIC_VISCOSITY','Pa*s',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('poise',0.1,0,'DYNAMIC_VISCOSITY','P',0.1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic foot per pound',0.0624279605761446,0,'SPECIFIC_VOLUME','ft3/lb',0.0624279605761446,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic centimeter per gram',0.001,0,'SPECIFIC_VOLUME','cm3/g',0.001,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('cubic meter per kilogram',1,0,'SPECIFIC_VOLUME','m3/kg',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('delta kelvin',1,0,'TEMPERATURE(DELTA)','delta K',1,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('delta degree Fahrenheit',0.555555555555556,0,'TEMPERATURE(DELTA)','delta °F',0.555555555555556,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('delta degree Rankine',0.555555555555556,0,'TEMPERATURE(DELTA)','delta °R',0.555555555555556,0);
insert into uoms(name, ref_factor, ref_offset, lookup_code,abbreviation, canonical_factor, canonical_offset) values ('delta degree Celsius',1,0,'TEMPERATURE(DELTA)','delta °C',1,0);
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'count';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'coulomb';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='coulomb' and uoms.name = 'Ampere hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'kilogram per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound per second' and uoms.name = 'long ton per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound per second' and uoms.name = 'million pound per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound per second' and uoms.name = 'short ton per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound per second' and uoms.name = 'thousand pound per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per second' and uoms.name = 'gram per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per second' and uoms.name = 'pound per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per second' and uoms.name = 'tonne per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram' and uoms.name = 'gram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram' and uoms.name = 'milligram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram' and uoms.name = 'pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram' and uoms.name = 'tonne';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'long ton';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'million pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'ounce';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'short ton';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'thousand pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pound' and uoms.name = 'ton';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'kilogram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='minute' and uoms.name = 'hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='day' and uoms.name = 'month';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='day' and uoms.name = 'week';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='day' and uoms.name = 'year';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='hour' and uoms.name = 'day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='second' and uoms.name = 'minute';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='newton' and uoms.name = 'dyne';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='newton' and uoms.name = 'kilogram-force';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='newton' and uoms.name = 'pound-force';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'newton';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='British thermal unit per hour' and uoms.name = 'million British thermal unit per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='British thermal unit per hour' and uoms.name = 'million British thermal unit per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'watt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'gigawatt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'megawatt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'British thermal unit per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'calorie per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'horsepower';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'joule per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'kilowatt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'megajoule per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='watt' and uoms.name = 'million calorie per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'mole per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole per second' and uoms.name = 'gram mole per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole per second' and uoms.name = 'kilogram mole per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole per second' and uoms.name = 'pound mole per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter' and uoms.name = 'centimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter' and uoms.name = 'inch';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter' and uoms.name = 'International nautical mile';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter' and uoms.name = 'kilometer';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter' and uoms.name = 'millimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='inch' and uoms.name = 'foot';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='inch' and uoms.name = 'mile';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='inch' and uoms.name = 'sixteenth of an inch';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='inch' and uoms.name = 'yard';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'candela';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'meter per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter per second' and uoms.name = 'centimeter per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter per second' and uoms.name = 'foot per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter per second' and uoms.name = 'International nautical mile per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter per second' and uoms.name = 'kilometer per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='meter per second' and uoms.name = 'mile per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='radian per second' and uoms.name = 'revolution per minute';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'radian per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kelvin' and uoms.name = 'British thermal unit per degree Rankine';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kelvin' and uoms.name = 'British thermal unit per degree Fahrenheit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kelvin' and uoms.name = 'kilojoule per kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'joule per kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'cubic meter per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'barrel per day';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'cubic centimeter per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'cubic foot per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'cubic meter per hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'Imperial gallon per minute';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'liter per second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per second' and uoms.name = 'US gallon per minute';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'pascal';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'atmosphere';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'bar';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'inches of mercury';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'kilogram-force per square centimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'kilogram-force per square meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'kilopascal';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'millimeter of mercury';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'newton per square meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'pound-force per square inch';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'pound-force per square inch (customary)';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal' and uoms.name = 'torr';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'square meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square meter' and uoms.name = 'hectare';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square meter' and uoms.name = 'square centimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square meter' and uoms.name = 'square inch';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square meter' and uoms.name = 'square kilometer';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square meter' and uoms.name = 'square millimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square inch' and uoms.name = 'square foot';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square foot' and uoms.name = 'acre';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square foot' and uoms.name = 'square mile';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='square foot' and uoms.name = 'square yard';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kelvin' and uoms.name = 'degree Celsius';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='degree Fahrenheit' and uoms.name = 'degree Rankine';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='degree Celsius' and uoms.name = 'degree Fahrenheit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='ampere' and uoms.name = 'milliampere';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'ampere';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'joule per kilogram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram' and uoms.name = 'joule per gram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram' and uoms.name = 'British thermal unit per pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram' and uoms.name = 'kilocalorie per kilogram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram' and uoms.name = 'kilojoule per kilogram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram' and uoms.name = 'kilojoule per pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'hertz';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram kelvin' and uoms.name = 'British thermal unit per pound degree Rankine';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram kelvin' and uoms.name = 'British thermal unit per pound degree Fahrenheit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram kelvin' and uoms.name = 'joule per gram kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule per kilogram kelvin' and uoms.name = 'kilojoule per kilogram kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'joule per kilogram kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole' and uoms.name = 'gram mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole' and uoms.name = 'kilogram mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='mole' and uoms.name = 'pound mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='percent' and uoms.name = 'parts per billion';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='percent' and uoms.name = 'parts per million';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'percent';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'kilogram per mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per kilogram mole' and uoms.name = 'gram per gram mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per kilogram mole' and uoms.name = 'pound per pound mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per mole' and uoms.name = 'kilogram per kilogram mole';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'volt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='volt' and uoms.name = 'kilovolt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='volt' and uoms.name = 'megavolt';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'gigawatt hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'megawatt hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'watt hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'joule';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'British thermal unit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'calorie';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'gigajoule';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'kilojoule';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'kilowatt hour';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'megajoule';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='joule' and uoms.name = 'watt second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='calorie' and uoms.name = 'kilocalorie';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='calorie' and uoms.name = 'million calorie';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='British thermal unit' and uoms.name = 'million British thermal unit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='Imperial gallon' and uoms.name = 'million imperial gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='Imperial gallon' and uoms.name = 'thousand imperial gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='US gallon' and uoms.name = 'barrel';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='US gallon' and uoms.name = 'Imperial gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='US gallon' and uoms.name = 'million US gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='US gallon' and uoms.name = 'thousand US gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'cubic centimeter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'cubic foot';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'kiloliter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'liter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'megaliter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'milliliter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'thousand cubic meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter' and uoms.name = 'US gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='barrel' and uoms.name = 'million barrel';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='barrel' and uoms.name = 'thousand barrel';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic foot' and uoms.name = 'acre foot';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'cubic meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'ohm';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'kilogram per cubic meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'gram per liter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'kilogram per liter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'pound per barrel';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'pound per cubic foot';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'pound per US gallon';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='kilogram per cubic meter' and uoms.name = 'tonne per cubic meter';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'radian';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='radian' and uoms.name = 'degree';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='radian' and uoms.name = 'revolution';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'pascal second';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='pascal second' and uoms.name = 'poise';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per kilogram' and uoms.name = 'cubic foot per pound';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='cubic meter per kilogram' and uoms.name = 'cubic centimeter per gram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'cubic meter per kilogram';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='NULL' and uoms.name = 'delta kelvin';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='delta degree Celsius' and uoms.name = 'delta degree Fahrenheit';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='delta degree Celsius' and uoms.name = 'delta degree Rankine';
update uoms set ref_id = u1.id from uoms u1 where u1.name ='delta kelvin' and uoms.name = 'delta degree Celsius';
