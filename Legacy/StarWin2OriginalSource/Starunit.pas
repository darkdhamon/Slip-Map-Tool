UNIT STARUNIT;

INTERFACE

CONST  max_unusual= 40;
       max_abilities= 96;

       class_spec : ARRAY [1..22] of STRING[30] =
       ('A','M','F','F','G','K','M','M','F',
       'M','B','M','O','','Nebula','Pulsar','Black hole','Quasar','Ion storms',
       'Primary Star Characteristics','Companion Star Characteristics',
       'Space Rift');

       atmos_genre : ARRAY [1..6] of STRING[10] =
       ('Massive  ','Dense    ','Standard ','Thin     ','Very thin',
       'Vacuum   ');

       world_genre : ARRAY [1..23] of STRING[15] =
       ('Ice Ball     ','Rock         ','Gas Giant    ','Hot House    ',
       'Failed Core  ','Asteroid Belt','Chunk        ','Arid         ',
       'Steppe       ','Terran       ','Jungle       ','Ocean        ',
       'Desert       ','Glacier      ','Nickel-Iron  ','Stony        ',
       'Carbonaceous ','Icy          ','Ring         ','Brown Dwarf  ',
       'Post Garden  ','Pre Garden   ','Tundra       ');

       world_genre2 : ARRAY [1..23] of STRING[15] =
       ('Ice Ball','Rock','Gas Giant','Hot House',
       'Failed Core','Asteroid Belt','Chunk','Arid',
       'Steppe','Terran','Jungle','Ocean',
       'Desert','Glacier','Nickel-Iron','Stony',
       'Carbonaceous','Icy','Ring','Brown Dwarf',
       'Post Garden','Pre Garden','Tundra');

       water_genre : ARRAY [1..6] of STRING[15] =
       ('None','Rare Ice','Ice ','Crystals','Oceans','Ice Sheets');

       atmos_comp : ARRAY [1..21] of STRING[40] =
       ('None','Nitrogen/Oxygen','Carbon Dioxyde','Nitrogen','Chlorine',
       'Methane/Ammonia/Hydrogen','Ammonia','Methane','Hydrogen Peroxyde/Nitrogen',
       'Exotic','Carbon Dioxyde/Sulfur Dioxyde','Nitrogen/Chlorine',
       'Nitric Acid/Carbon Dioxyde','Hydrogen Peroxyde/Hydrogen Sulfide',
       'Nitrogen/Carbon Dioxyde','Methane/Ammonia','Chlorine/Carbon Dioxyde',
       'Chlorine/Disulfur Dichloride','Nitrogen/Sulfuric Acid','Hydrogen',
       'Methane/Water Vapor');

       atmos_breath : ARRAY [1..21] of STRING[40] =
       ('None','Oxygen','Carbon Dioxyde','Nitrogen','Chlorine',
       'Ammonia','Ammonia','Methane','Nitrogen','Exotic','Carbon Dioxyde',
       'Chlorine','Carbon Dioxyde','Hydrogen Sulfide','Carbon Dioxyde',
       'Ammonia','Chlorine','Chlorine','Nitrogen','Hydrogen',
       'Methane');


       belt_width : ARRAY [1..11] of REAL =
       (0.01,0.05,0.1,0.1,0.5,0.5,1,1.5,2,5,10);

       unusual_genre : ARRAY [1..max_unusual] of STRING[30] =
       ('Extreme Vulcanism','Atmos. Contaminants','Meteors Storms',
       'High Radiation Level','Violent Storms','Microbes',
       'Orbital Conjunction','Rugged Terrain','Retrograde Rotation',
       'Unstable Climate','Orbital Eccentricity','Unstable World',
       'Strong Magnetic Field','Cloud Cover','No Axial Tilt','High Tides',
       'Tidal Lock','Extreme Axial Tilt','Int. Lifeforms',
       'Semi-Int. Lifeforms','High Humidity','Low Humidity',
       'Corrosive Atmosphere','Insidious Atmosphere',
       'Twin World','Roche World','Climatic Vortex','Alien artifacts',
       'Recent city ruins','Remains of dead civ.','Space cemetery',
       'Wonder of the galaxy','Holy site','Proto-organisms','Primitive Lifeforms',
       'High population','Terraformed','u38','u39','u40');

       environment_table : ARRAY [1..5] of STRING[15]=
       ('Land-dwelling','Burrowing','Amphibious','Aquatic','Flying');

       color: ARRAY [1..19] of STRING[15] =
       ('White','Black','Yellow','Red','Gray','Blue','Green','Brown',
       'Pink','Orange','Crimson','Violet','Clear','Calico','Silver','Gold',
       'Hazel','Blonde','Chestnut');

       body_part: ARRAY [1..16] of string[15]=
       ('Tail','Trunk','Horn(s)','Antennas','Fangs',
       'Claws','Nippers','Hooves','Jelly bag','bp10',
       'One color','Two-tone','Multi color','Striped/Banded',
       'Spots','Randomly motted');

       eyes_part: ARRAY [1..14] of string[15]=
       ('No eyes','Single','Three','Four','Multiple','Large','Double-lided',
       'Bulging','Luminous','Stalked','Round pupil','Slitted pupil','No pupil',
       'Multi faceted');

       hair_part: ARRAY [1..8] of string[15]=
       ('No hair','Rare','Bony crest','Palps','Crest','Fur','Feather','Short');

       body_table : ARRAY [1..9] of STRING[25]=
       ('Carbon','Silicon non-crystalline','Sulfur','Exotic','Liquid','Silicon crystalline',
        'Metallic crystalline','Gaseous','Energy');

       body_cover: ARRAY [1..12] of STRING[20]=
       ('Soft-skinned','Thick-skinned','Furred','Feathered','Scaled',
       'Spiny','Hard-shelled','Stone-skinned','Miscleanous','Metallic','Crystal-skinned',
       'Cellulose-skinned');

       limbs_type: ARRAY [0..7] of STRING[30]=
       ('None','Wings','Fins','Legs','Dual-purpose arm/legs','Arms','Tentacles',
        'Pseudopods');

       diet_type: ARRAY [1..8] of STRING[20]=
       ('Herbivore','Omnivore','Carnivore','Special','Parasite','Solar Energy','Thermal Energy',
        'No Feeding');

       reproduction_type: ARRAY [1..5] of STRING[20]=
       ('Asexual','Hermaphroditic','Two sexes','Three sexes','FMN');

       repro_methode_type: ARRAY [1..4] of STRING[20]=
       ('External budding','Egg-laying','Live-bearing','Parasitic');

       appearance_type: ARRAY [1..26] of STRING[30]=
       ('Humanoid     ','Insectoid    ','Reptilian    ','Canine       ',
        'Feline       ','Picthinine   ','Ursoid       ','Vegetal      ',
        'Mineral      ','Avian        ','Amphibian    ','Animal       ',
        'Totally alien','Centauroid   ','Amoebic      ','Serpentile   ',
        'Energetic    ','Mechanoid    ','Geometric    ','Crustacean   ',
        'Arachnid     ','Porcine      ','Rodent       ','Liquid       ',
        'Gaseous      ','Radial       ');

       charac: ARRAY [1..15] of STRING[20] =
       ('Militancy       : ','Determination   : ','Racial tolerance: ',
       'Progressivness  : ','Loyalty         : ','Social cohesion : ',
       'Psi power       : ','Tech level      : ','Body            : ',
       'Mind            : ','Speed           : ','Lifespan        : ',
       'Art             : ','Individualism   : ','Spatial Age     : ');

       special_ability: ARRAY [1..max_abilities] of STRING[30] =
       ('Acute hearing','Poor hearing','Acute smell','Acute vision',
       'Poor vision','Ambidextrous','Chameleon skin','Cold sensitivity',
       'Cold tolerance','Color blind','Heat sensitivity','Heat tolerance',
       'Infrared vision','Night vision','Poison','Radiation tolerance',
       'Fast healing','Sonar','Wall climbing','Web spinning','Nictating membrane',
       'Radio hearing','Acid secretion','Metamorphosis','Electric blast',
       'Hypnotism','Mimicry','Dampen','360 degrees vision','Sonic beam',
       'Vampirism','Slow motion','Sealed system','Clone','Stretching',
       'Systemic antidote','Mystical power','Independent eyes','Quick maturity',
       'Infertile','Hive mind','Bicephalous','Regeneration','Racial memory',
       'Universal digestion','Pressure support','Poloarized eyes',
       'Vulnerability to disease','Cultural adaptability','Field sense',
       'Cold blooded biology','Winged Flight','Water breathing','Flight',
       'Charisma','Spectrum vision','Ultrasonic hearing','Microscopic vision','Blind',
       'Deafness','Odious racial habit','Merchant bonus skill',
       'Engineer bonus skill','Pilot bonus skill','Combat bonus skill',
       'Science bonus skill','Water dependency','Light sensitivity','Involuntary dampen',
       'Sound sensitivity','Disease tolerance','Eidetic memory','Language talent',
       'No sense of smell/taste','Strange appearance','Manual dexterity',
       'Perfect balance','Foul odor','Skin color change','Dependency',
       'High fecundity','Cybernetic enhancements','Computer skill bonus',
       'Leap','Vibration sense','Toughness','High gravity sensitivity',
       'Toxin intolerance','Extra Heart','Heavy sleeper','Light sleeper',
       'Chemical communication','Lightening calculator','Time sense','EM Imaging',
       'Ultrasonic communication');

       gov: ARRAY [1..27] of STRING[20] =
       ('Anarchy','Tribalism','Community','Democracy','Balkanization',
       'Monarchy','Theocracy','Corporation','Oligarchy','Technocracy',
       'Bureaucracy','Dictatorship','Republic','Imperialism','Matriarchy',
       'Ploutocracy','Gerontocracy','Aristocracy','Meritocracy',
       'Stochastic','Utopia','Federation','Syndicate','Computer Oligarchy',
       'Gaming Oligarchy','Subjugated colony','Colony');

       mineral_name: ARRAY [1..5] of STRING[20] =
       ('Metal ore      ','Radioactive ore','Precious metal ',
       'Raw crystals   ','Precious gems  ');

       religion_genre: ARRAY [1..11] of STRING[30] =
       ('Animism','Polytheism','Dualism','Monotheism','Deism',
        'Pantheism','Agnosticism','Rational atheism','Philosophical atheism',
        'Leader worship','Multiple monotheism');

       colony_genre: ARRAY [1..11] of STRING[20] =
       ('Agricultural','High population','Industrial','Mining','Fluid',
        'Recreational','Capital','Homeworld','Settlement','Military','Research');

       starport_genre: ARRAY [1..5] of STRING[10] =
       ('Excellent','Good','Fair','Primitive','None');

       relation_genre: ARRAY [1..5] of STRING[20] =
       ('War','No intercourse','Trade','Alliance','Unity');

       facilities_genre: ARRAY [1..16] of STRING[20] =
       ('Military Base','Naval Base','Prison Camp','Exile Camp','University',
       'Military Academy','Arcology','Orbital tower','Ringworld','Planet shield',
       'Space habitats','f12','f13','f14','f15','f16');

       eco_genre: ARRAY [0..18] of STRING[30] =
       ('None','Agricultural resources','Mineral resources','Compounds','Agroproducts',
       'Processed ores','Processed compounds','Weapons','Consumables','Pharmaceuticals',
       'Durable goods','Hi-Tech goods','Artforms','Recordings','Software',
       'Scientific datas','Exotic natural resource','Prototypes Mfd goods','Uniques');

IMPLEMENTATION

END.



