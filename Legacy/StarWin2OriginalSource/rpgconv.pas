{ RPG conversion v1.6   (c) Aina Rasolomalala  03-2001

 v1.57b Add Spacemaster, Fuzion conversion
 v1.57c HardCore fuzion plug-in added
 v1.6   Html output}


unit rpgconv;

interface

uses recunit,math;

procedure fuzion_conversion (sys4:alien_record;output_form:byte;var f:text);
procedure alternity_conversion (sys4:alien_record;output_form:byte;var f:text);
procedure battle_conversion (sys4:alien_record;output_form:byte;var f:text);
procedure gurps_conversion (sys4:alien_record;output_form:byte;var f:text);
procedure master_conversion (sys4:alien_record;output_form:byte;var f:text);

implementation

uses starunit;

const gurps_stat : ARRAY [1..4] of STRING[2] =
          ('ST','DX','IQ','HT');

      gurps_special_ability: ARRAY [1..max_abilities] of STRING[40] =
       ('Acute hearing (lvl 3)','Hard of hearing','Acute taste/smell (lvl 3)',
       'Acute vision (lvl 3)',
       'Bad sight','Ambidexterity','Chameleon','Cold weakness',
       'Cold tolerance (heat)','Color blindness','Heat weakness',
       'Temperature tolerance (heat)',
       'Infravision','Night vision','Poisonous venom','Damage resistance (Radiation lvl 5)',
       'Regeneration','Sonar vision','Clinging','Web spinning','Nictating membrane',
       'Radio hearing','Venom (corrosive)','Morph','Lightning',
       'Hypnotism at IQ +1','Mimicry','Dampen','360 degrees vision','Sonic blast',
       'Blood healing','Slow metabolism','Doesn''t breathe','Clone','Stretching',
       'Immunity to poison','Magical Aptitude','Independently focusable eyes',
       'Early Maturation',
       'Dying race','Mindshare (Hive mind)','Bicephalous','Regrowth','Racial memory',
       'Universal digestion','Pressure support','Poloarized eyes',
       'Vulnerability (disease, occasional)','Cultural adaptability','Field sense',
       'Metabolism control (lvl 2)','Winged flight','Gills','Winged flight',
       'Charisma +2','Spectrum vision','Ultrasonic hearing','Microscopic vision (x4)','Blind',
       'Deafness','Odious racial habit','Merchant +1',
       'Engineer +1','Pilot +1','Combat skill +1',
       'Science skill +1','Water dependency','Light sensitivity','Involuntary dampen',
       'Sound sensitivity','Immunity to disease','Eidetic memory','Language talent +2',
       'No sense of smell/taste','Appearance','Manual dexterity',
       'Perfect balance','Foul odor','Skin color change','Dependency (common weekly)',
       'High fecundity','Cybernetic enhancements','Computer programming +1',
       'Jumping at DX+2','Sensitive touch','Extra Hit Points +1','G-Intolerance',
       'Toxin weakness','Extra Fatigue +1','Sleepy 50%','Doesn''sleep',
       'Chemical communication','Lightening calculator','Time sense','EM Imaging',
       'Ultrasonic speech');


      gurps_ability_cost: ARRAY [1..max_abilities+32] of shortint =
       (6,-10,6,6,-10,10,10,-10,10,-10,-10,10,15,10,15,10,25,25,30,15,15,15,15,40,
        35,6,15,15,25,35,25,-10,30,0,15,25,15,15,10,-10,20,15,40,40,15,5,5,-10,25,10,
        10,30,0,40,10,40,10,8,-50,-20,-10,2,3,2,2,4,-5,-30,-15,-20,10,30,4,-5,-20,
        10,25,-10,0,-10,5,15,4,4,15,8,-10,-10,5,-10,10,25,5,5,30,25,
        20,4,6,15,25,25,4,8,70,20,20,60,5,40,50,3,
        -40,-50,-5,-50,-25,-10,-10,-15,-30,-5,-15,-10,-10,-10,00,00);

      gurps_attribute_cost: ARRAY [1..20] of smallint =
       (-80,-70,-60,-50,-40,-30,-20,-15,-10,0,10,20,30,45,60,80,100,125,150,175);

      gurps_misc_adv: ARRAY [1..16] of string[40] =
       ('Amphibious','Damage resistance +1','Body of stone','Claws','Extended lifespan',
        'Increased speed','Strong will','Damage resistance +2','Feet manipulators',
        'Cutting Strikers x2','High Technology +1TL','Extra arms','Extra legs',
        'Extra arms (cannot strike)','Psionics','Extra limb (striker, accuracy -2,tail)');
      gurps_misc_disadv: ARRAY [1..16] of string[40] =
       ('Aquatic','No manipulators','Reduced move','Sessile','Short lifespan',
        'Weak will','Intolerance','Pacifism','No fine manipulators','Primitive',
        'Fanatic','Sense of duty','Bloodlust','Laziness','d15','d16');

      battle_stat: ARRAY [1..12] of string[22] =
       ('Str','Manual Dexterity','IQ','Agility','Constitution','Aggression',
        'Intuition','Charisma','Terrestrial knowledge','Military Leadership',
        'Persuasion','Bargaining');

      battle_smr: ARRAY [1..10] of string[3] =
       ('CHE','RAD','BIO','MEN','POI','SON','ELE','FIR','ACD','CLD');

      battle_smr_cst: ARRAY [1..10] of byte =
       (15,15,10,45,20,20,40,20,20,40);

      battle_ability: ARRAY [1..max_abilities] of STRING[40] =
       ('Exceptional hearing','Limited hearing','Excellent smell','Exceptional sight',
       'Limited sight','Ambidextrous','Chameleon skin','Susceptibility to cold',
       'Cold tolerance','Color blind','Susceptibility to heat','Heat tolerance',
       'Infravision','Night vision','Poison','Radiation tolerance',
       'Fast healing','Sonar','Wall climbing','Web spinning','Nictating membrane',
       'Radio hearing','Acid secretion','Shape Change','Electric blast',
       'Hypnotism','Mimicry','Dampen','360 degrees vision','Sonic beam',
       'Vampirism','Slow motion','Sealed system','Clone','Stretching',
       'Immune to poison','Mystical power','Independent eyes','Quick maturity',
       'Infertile','Hive mind','Bicephalous','Regeneration','Racial memory',
       'Universal digestion','Pressure support','Poloarized eyes',
       'Susceptiblity to disease','Cultural adaptability','Field sense',
       'Extreme cold causes hibernation','Winged Flight','Water breathing','Flight',
       'Charisma','Spectrum vision','Ultrasonic hearing','Microscopic vision','Blind',
       'Deafness','Odious racial habit','Merchant skills cost one less',
       'Engineering skills cost one less','Piloting skills cost one less',
       'One Combat skill costs one less','Sciences skills cost one less',
       'Water dependency','Light sensitivity','Involuntary dampen',
       'Sound sensitivity','Disease tolerance','Eidetic memory','Language talent',
       'No sense of smell/taste','Strange appearance','1st level pick locks',
       '3rd level acrobatics','Foul odor','Skin color change','Dependency',
       'High fecundity','Cybernetic enhancements','Computers skills cost one less',
       'Leap','Vibration sense','Toughness','High gravity sensitivity',
       'Sensitive to toxin','Extra Heart','Heavy sleeper','Light sleeper',
       'Chemical communication','Lightening calculator','Time sense','EM Imaging',
       'Ultrasonic communication');

      battle_misc_ability: ARRAY [1..7] of string[40] =
       ('Matrix control','5th level swimming','8th level swimming',
        '1 point body threshold','2 points body threshold','3 points body threshold',
        'Fires multiple weapons');


procedure fuzion_conversion (sys4:alien_record;output_form:byte;var f:text);
var   tmp,total_EP,result:smallint;
      a,legs_nbr,fins_nbr,arms_nbr,tenta_nbr,dual_nbr,pseudo_nbr,nipper:byte;
      complic:boolean;
      b1,b2:string;
const fuzion_attribute: ARRAY [1..10] of STRING[4] =
        ('INT','WILL','PRE','TECH','REF','DEX','CON','STR','BODY','MOVE');
     fuzion_form: ARRAY [1..7] of STRING[30]=
       ('Multicellular Carbon Based','Silicon Non-crystalline','Liquid','Silicon crystalline',
        'Metallic crystalline','Gaseous','Pure Energy');
     fuzion_form_ep: ARRAY [1..7] of byte=
       (4,8,20,8,1,15,40);
     fuzion_exterior: ARRAY [1..5] of STRING[15]=
       ('Skin','Fur','Scales','Feathers','Shell');
     fuzion_exterior_ep: ARRAY [1..5] of byte=
       (1,2,5,4,5);
     fuzion_cardio: ARRAY [1..3] of STRING[30]=
       ('Closed Centralized, 1 heart','Closed Centralized, 2 heart','Osmosis');
     fuzion_cardio_ep: ARRAY [1..3] of byte=
       (5,8,15);
     fuzion_fluid: ARRAY [1..5] of STRING[20]=
       ('Cold blooded','Warm blooded','Chlorophyll','Acidic - 7DC damage','Energy');
     fuzion_fluid_ep: ARRAY [1..5] of byte=
       (2,4,2,11,5);
     {v1.57c: Air lungs => Air lungs,hold breath 5 min 6EP}
     fuzion_resp: ARRAY [1..8] of STRING[30]=
       ('Absorption (air)','Absorption (water)','Absorption (air/water)','Water gills',
        'Water lung','Air Lungs, hold breath 5 min','No respiration','Water gills & Air lungs');
     fuzion_resp_ep: ARRAY [1..8] of byte=
       (1,1,2,2,3,6,15,7);
     fuzion_loco: ARRAY [1..9] of STRING[20]=
       ('Full Swimming','Partial Swimming','Winged Flight','Glide Flight','Aqua Jets',
        'Slither','Biped (Lateral)','Quadruped','n-taped');
     fuzion_feed_ep: ARRAY [1..8] of byte=
       (1,4,3,3,0,3,3,15);

begin
   writeln(f);
   case output_form of
     0,1: begin
            b1:='';
            b2:='';
          end;
     2  : begin
            b1:='<center><b>';
            b2:='</b></center>';
          end;
   end;
   writeln(f,b1,' FUZION conversion',b2);
   if output_form<>2 then writeln(f,' *****************');
   writeln(f);
   total_ep:=0;
   write(f,'Form : ');
   case sys4.body_type of
     1,3,4: tmp:=1;
     2    : tmp:=2;
     5    : tmp:=3;
     6    : tmp:=4;
     7    : tmp:=5;
     8    : tmp:=6;
     9    : tmp:=7;
   end;
   total_ep:=fuzion_form_ep[tmp];
   writeln(f,fuzion_form[tmp],',',sys4.mass,'kg ave., ',total_ep,' EP');
   write(f,'Physical Exterior : ');
   case sys4.body_cover_type of
     1,2,6: tmp:=1;
     3    : tmp:=2;
     4    : tmp:=4;
     5    : tmp:=3;
     7    : tmp:=5;
     else  tmp:=0;
   end;
   if tmp=0 then writeln(f,'None')
     else begin
            writeln(f,fuzion_exterior[tmp],', ',fuzion_exterior_ep[tmp],' EP');
            total_ep:=total_ep+fuzion_exterior_ep[tmp];
          end;
   case sys4.body_type of
     1,3 : begin
             if is_set(sys4.table_abil[12],1) then tmp:=2 else tmp:=1;
           end;
     else   tmp:=3;
   end;
   writeln(f,'Cardiovascular : ',fuzion_cardio[tmp],', ',fuzion_cardio_ep[tmp],' EP');
   total_ep:=total_ep+fuzion_cardio_ep[tmp];
   case sys4.body_type of
     1,3 : begin
             if is_set(sys4.table_abil[7],3) then tmp:=1 else tmp:=2;
          end;
     else tmp:=5;
   end;
   if sys4.app_genre=8 then tmp:=3;
   if is_set(sys4.table_abil[3],7) then tmp:=4; {Acidic blood'}
   writeln(f,'        Fluid Type: ',fuzion_fluid[tmp],', ',fuzion_fluid_ep[tmp],' EP');
   total_ep:=total_ep+fuzion_fluid_ep[tmp];
   case sys4.environment_type of
     1,2,5: begin
              if sys4.app_genre=8 then tmp:=1
                 else tmp:=6;
            end;
     4    : begin
              if sys4.app_genre=8 then tmp:=2
                 else tmp:=4;
            end;
     3    : begin
              if sys4.app_genre=8 then tmp:=3
                 else tmp:=8;
            end;
   end;
   if is_set(sys4.table_abil[5],1) then tmp:=7;
   if (tmp=4) and (sys4.app_genre=13) then tmp:=5;
   writeln(f,'Respiratory : ',fuzion_resp[tmp],', ',fuzion_resp_ep[tmp],' EP');
   total_ep:=total_ep+fuzion_resp_ep[tmp];
   writeln(f,'Bio-Stats');
   write(f,'        Life-Span : ');
   if sys4.attrib[12]>=40 then
       begin
         writeln(f,'Immortal Life, 40 EP');
         total_ep:=total_ep+40;
       end
     else
       begin
         writeln(f,sys4.attrib[12]*5,' years, ',sys4.attrib[12],' EP');
         total_ep:=total_ep+sys4.attrib[12];
       end;
   write(f,'        Sleep-Time : ');
   if is_set(sys4.table_abil[12],2) then tmp:=3
      else if is_set(sys4.table_abil[12],3) then tmp:=10
          else tmp:=5;
   total_ep:=total_ep+tmp;
   case tmp of
     3: writeln(f,'50% of the time, 3 EP');
     5: writeln(f,'33% of the time, 5 EP');
     8: writeln(f,'10% of the time, 8 EP');
   end;
   writeln(f,'        Vulnerabilities:');
   if is_set(sys4.table_abil[1],8) then
      begin
         writeln(f,'            Cold,Strong,Stunning,-3 EP');
         total_ep:=total_ep-3;
      end;
   if is_set(sys4.table_abil[2],3) then
      begin
         writeln(f,'            Heat,Strong,Stunning,-3 EP');
         total_ep:=total_ep-3;
      end;
   if is_set(sys4.table_abil[6],8) then
      begin
         writeln(f,'            Disease,Severe,Stunning,-3 EP');
         total_ep:=total_ep-3;
      end;
   if is_set(sys4.table_abil[9],4) then
      begin
         writeln(f,'            Light,Mild,Stunning,-3 EP');
         total_ep:=total_ep-3;
      end;
   if is_set(sys4.table_abil[9],6) then
      begin
         writeln(f,'            Noise,Strong,Stunning,-3 EP');
         total_ep:=total_ep-3;
      end;
   if is_set(sys4.table_abil[11],8) then
      begin
         writeln(f,'            Toxins,Extreme,Stunning,-4 EP');
         total_ep:=total_ep-4;
      end;
   if is_set(sys4.table_abil[2],8) then
      begin
         writeln(f,'            Radiation,Strong,Stunning,-2 EP');
         total_ep:=total_ep-2;
      end
     else
      begin
         writeln(f,'            Radiation,Extreme,Stunning,-4 EP');
         total_ep:=total_ep-4;
      end;
   if is_set(sys4.table_abil[5],1) then
      begin
         if (not is_set(sys4.table_abil[6],6)) then
             begin
                writeln(f,'            Vacuum,Mild,Killing,-4 EP');
                total_ep:=total_ep-4;
             end;
      end
     else
      begin
         writeln(f,'            Vacuum,Strong,Killing,-5 EP');
         total_ep:=total_ep-5;
      end;
   writeln(f,'        Immunities:');
   if is_set(sys4.table_abil[11],7) then
      begin
         writeln(f,'            G-Forces, 6 Gs, 3 EP');
         total_ep:=total_ep+3;
      end
     else
      begin
         if is_set(sys4.table_abil[6],6) then
             begin
               writeln(f,'            G-Forces, 10 Gs, 5 EP');
               total_ep:=total_ep+5;
             end
            else
             begin
               writeln(f,'            G-Forces, 8 Gs, 4 EP');
               total_ep:=total_ep+4;
             end;
      end;
   if is_set(sys4.table_abil[5],4) then
      begin
         writeln(f,'            All Poisons, 15 EP');
         total_ep:=total_ep+15;
      end;
   if is_set(sys4.table_abil[9],7) then
      begin
         writeln(f,'            All Diseases, 15 EP');
         total_ep:=total_ep+15;
      end;
   writeln(f,'Locomotion : ');
   legs_nbr:=0;
   fins_nbr:=0;
   arms_nbr:=0;
   tenta_nbr:=0;
   dual_nbr:=0;
   pseudo_nbr:=0;
   for a:=1 to sys4.limbs_number do
       begin
          if sys4.limbs_genre[a]=3 then legs_nbr:=legs_nbr+1;
          if sys4.limbs_genre[a]=4 then dual_nbr:=dual_nbr+1;
          if sys4.limbs_genre[a]=2 then fins_nbr:=fins_nbr+1;
          if sys4.limbs_genre[a]=5 then arms_nbr:=arms_nbr+1;
          if sys4.limbs_genre[a]=6 then tenta_nbr:=tenta_nbr+1;
          if sys4.limbs_genre[a]=7 then pseudo_nbr:=pseudo_nbr+1;
       end;

   if (sys4.environment_type=4) or (sys4.environment_type=3) then
      begin
         if (fins_nbr>0) or (sys4.environment_type=3) then
            begin
               writeln(f,'        ',fuzion_loco[1],' 4 EP');
               total_ep:=total_ep+4;
            end
           else
            begin
               writeln(f,'        ',fuzion_loco[5],' 5 EP');
               total_ep:=total_ep+5;
            end;
      end;
   if (legs_nbr+dual_nbr)=1 then
      begin
          writeln(f,'        ',fuzion_loco[7],' 4 EP');
          total_ep:=total_ep+4;
      end;
   if (legs_nbr+dual_nbr)=2 then
      begin
          writeln(f,'        ',fuzion_loco[8],' 4 EP');
          total_ep:=total_ep+4;
      end;
   if (legs_nbr+dual_nbr)>2 then
      begin
          writeln(f,'        ',fuzion_loco[9],' ',((legs_nbr+dual_nbr)-2)*2+4,' EP');
          total_ep:=total_ep+4+((legs_nbr+dual_nbr)-2)*2;
      end;
   if ((legs_nbr+dual_nbr)=0) and (sys4.environment_type<4) and (sys4.attrib[11]>2) then
      begin
          writeln(f,'        ',fuzion_loco[6],' 2 EP');
          total_ep:=total_ep+2;
      end;
   if sys4.environment_type<3 then
      begin
          writeln(f,'        ',fuzion_loco[2],' 2 EP');
          total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[7],4) then
      begin
          writeln(f,'        ',fuzion_loco[3],' 5 EP');
          total_ep:=total_ep+5;
      end;
   if (sys4.environment_type=5) and (not is_set(sys4.table_abil[7],4)) then
      begin
          writeln(f,'        ',fuzion_loco[4],' 3 EP');
          total_ep:=total_ep+3;
      end;
   if sys4.attrib[11]=0 then writeln(f,'        None, 0 EP');
   writeln(f,'Feeding Method : ',diet_type[sys4.diet_genre],' ',
       fuzion_feed_ep[sys4.diet_genre],' EP');
   total_ep:=total_ep+fuzion_feed_ep[sys4.diet_genre];
   writeln(f,'Sensory : ');
   if (not is_set(sys4.table_abil[8],3)) then
      begin
         if is_set(sys4.table_abil[2],2) then
             begin
                writeln(f,'        Sight, Optical (Color Blind), 2 EP');
                total_ep:=total_ep+2;
             end
           else
             begin
               if is_set(sys4.table_abil[1],4) then
                   begin
                      writeln(f,'        Sight, Optical (Acute Sense), 6 EP');
                      total_ep:=total_ep+6;
                   end
                 else
                   begin
                       writeln(f,'        Sight, Optical, 4 EP');
                       total_ep:=total_ep+4;
                   end;
             end;
         if is_set(sys4.table_abil[4],5) then
             begin
                writeln(f,'        360-Degree Sense (Sight), 3 EP');
                total_ep:=total_ep+3;
             end;
      end;
   if is_set(sys4.table_abil[2],6) then
      begin
         writeln(f,'        Sight, Night Vision, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[2],5) then
      begin
         writeln(f,'        Sight, Infrared, 6 EP');
         total_ep:=total_ep+6;
      end;
   if is_set(sys4.table_abil[7],8) then
      begin
         writeln(f,'        Sight, Radio Wave, 8 EP');
         writeln(f,'        Sight, Ultraviolet, 6 EP');
         total_ep:=total_ep+14;
      end;
   if is_set(sys4.table_abil[8],2) then   {v1.57c add}
      begin
         writeln(f,'        Sight, Microscopic vision, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[6],7) then   {v1.57c add}
      begin
         writeln(f,'        Sight, Poloarized eyes, 1 EP');
         total_ep:=total_ep+1;
      end;
   if is_set(sys4.table_abil[5],6) then   {v1.57c add}
          begin
              writeln(f,'        Independant eyes, 2 EP');
              total_ep:=total_ep+2;
          end;
   if (not is_set(sys4.table_abil[10],2)) then
      begin
          if is_set(sys4.table_abil[1],3) then
              begin
                  writeln(f,'        Smell (Acute Sense), 5 EP');
                  total_ep:=total_ep+2;
              end
            else  writeln(f,'        Smell, 3 EP');
         writeln(f,'        Taste, 2 EP');
         total_ep:=total_ep+5;
      end;
   if sys4.body_type<8 then
      begin
         writeln(f,'        Touch, Direct, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[11],5) then
      begin
         writeln(f,'        Touch, Ranged, 6 EP');
         total_ep:=total_ep+6;
      end;
   if (not is_set(sys4.table_abil[8],4)) then
      begin
          if is_set(sys4.table_abil[1],1) then
              begin
                  writeln(f,'        Hearing, Sonic (Acute Sense), 5 EP');
                  total_ep:=total_ep+2;
              end
            else writeln(f,'        Hearing, Sonic 3 EP');
         total_ep:=total_ep+3;
      end;
   if is_set(sys4.table_abil[8],1) then
      begin
         writeln(f,'        Hearing, Ultrasonic, 3 EP');
         total_ep:=total_ep+3;
      end;
   if is_set(sys4.table_abil[3],6) then
      begin
         if is_set(sys4.body_char[1],4) then
              begin
                writeln(f,'        Hearing, Radio (Antenna enhanced), 7 EP');
                total_ep:=total_ep+7;
              end
           else
              begin
                writeln(f,'        Hearing, Radio, 5 EP');
                total_ep:=total_ep+5;
              end;
      end;
   if is_set(sys4.table_abil[3],2) then
      begin
         if is_set(sys4.body_char[1],4) then
              begin
                writeln(f,'        Sonar (Antenna enhanced), 7 EP');
                total_ep:=total_ep+7;
              end
           else
              begin
                writeln(f,'        Sonar, 5 EP');
                total_ep:=total_ep+5;
              end;
      end;
   if is_set(sys4.table_abil[7],2) then
      begin
         writeln(f,'        Eletromagnetic Sense, 5 EP');
         total_ep:=total_ep+5;
      end;
   if is_set(sys4.table_abil[12],7) then
      begin
         writeln(f,'        EM Imaging, 8 EP');
         total_ep:=total_ep+8;
      end;
   writeln(f,'Communication : ');
   if (not is_set(sys4.table_abil[8],4)) then
      begin
         writeln(f,'        Vocal Communication, Sonic 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[12],8) then
      begin
         writeln(f,'        Vocal Communication, Ultrasonic 3 EP');
         total_ep:=total_ep+3;
      end;
   if is_set(sys4.table_abil[12],4) then
      begin
         writeln(f,'        Chemical Communication, 1 EP');
         total_ep:=total_ep+1;
      end;
   if is_set(sys4.table_abil[6],1) then
      begin
         writeln(f,'        Hive Mentality, 5 EP');
         total_ep:=total_ep+5;
      end;
   write(f,'Neural : ');
   case sys4.body_type of
     7: begin
           writeln(f,'Simiconductive, Centralized, 10 EP');
           total_ep:=total_ep+10;
        end;
     8: writeln(f,'Biochemical, Distributed, 0 EP');
     9: begin
           writeln(f,'Superconductive, Distributed, 25 EP');
           total_ep:=total_ep+25;
        end;
     else
        begin
           writeln(f,'Neuro-Electrochemical, Centralized, 5 EP');
           total_ep:=total_ep+5;
        end;
   end;
   if is_set(sys4.table_abil[9],8) then
      begin
         writeln(f,'        Eidetic Memory, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[12],5) then
      begin
         writeln(f,'        Lightening Calculator, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[12],6) then
      begin
         writeln(f,'        Time Sense, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[6],2) then
      begin
         writeln(f,'        Bicephalous, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[12],6) then
      begin
         writeln(f,'        Racial memory, 2 EP');
         total_ep:=total_ep+2;
      end;
   writeln(f,'Special Features ');
   if arms_nbr>0 then
      begin
          writeln(f,'        Secondary limbs, ',arms_nbr,' pair(s) ',4+(arms_nbr-1)*5,' EP');
          total_ep:=total_ep+4+(arms_nbr-1)*5;
      end;
   if is_set(sys4.table_abil[1],6) then
      begin
         writeln(f,'        Ambidexterity, ',arms_nbr*2,' EP');
         total_ep:=total_ep+arms_nbr*2;
      end;
   if pseudo_nbr>0 then
      begin
          writeln(f,'        Tentacles , ',pseudo_nbr,' pair(s) ',pseudo_nbr,' EP');
          total_ep:=total_ep+pseudo_nbr;
      end;
   if is_set(sys4.body_char[1],2) then
      begin
         writeln(f,'        Tentacle, Trunk, 2 EP');
         total_ep:=total_ep+2;
      end;
   if tenta_nbr>0 then
      begin
          writeln(f,'        Tentacles, ',tenta_nbr,' pair(s) ',tenta_nbr*4,' EP');
          total_ep:=total_ep+tenta_nbr*4;
      end;
   if is_set(sys4.body_char[1],7) then nipper:=1 else nipper:=0; {v1.57c add}
   if (arms_nbr+dual_nbr-nipper)>0 then
      begin
          writeln(f,'        Fine Manipulators ',arms_nbr+dual_nbr,' pair(s) ',
             6*(arms_nbr+dual_nbr),' EP');
          total_ep:=total_ep+6*(arms_nbr+dual_nbr);
      end;
   if is_set(sys4.body_char[1],1) then
      begin
         writeln(f,'        Tentacle, Tail, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[3],1) then
      begin
         writeln(f,'        Rapid Regeneration, 10 EP');
         total_ep:=total_ep+10;
      end;
   if is_set(sys4.table_abil[6],3) then
      begin
         writeln(f,'        Regrowth level 2, 20 EP');
         total_ep:=total_ep+10;
      end;
   case sys4.body_cover_type of
       2: begin
             writeln(f,'        Natural armor 3KD, 3 EP');
             total_ep:=total_ep+3;
          end;
       5: begin
              writeln(f,'        Natural armor 8KD, 8 EP');
              total_ep:=total_ep+8;
          end;
       7: begin
              writeln(f,'        Natural armor 15KD, 15 EP');
              total_ep:=total_ep+15;
          end;
       8,10,11: begin
                   writeln(f,'        Natural armor 25KD, 25 EP');
                   total_ep:=total_ep+25;
                end;
    end;
   if is_set(sys4.table_abil[1],7) then
      begin
         writeln(f,'        Chameleon Level 2, 10 EP');
         total_ep:=total_ep+10;
      end;
   if sys4.body_cover_type=6 then
      begin
         writeln(f,'        Spines, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.body_char[1],3) then
      begin
         writeln(f,'        Spikes (horn), 4 EP');
         total_ep:=total_ep+4;
      end;
   if is_set(sys4.body_char[1],6) then
      begin
         writeln(f,'        Claws, ',(arms_nbr+dual_nbr)*6,' EP');
         total_ep:=total_ep+(arms_nbr+dual_nbr)*6;
      end;
   if is_set(sys4.body_char[1],7) then
      begin
         writeln(f,'        Pincer Claws, ',arms_nbr*8,' EP');
         total_ep:=total_ep+arms_nbr*8;
      end;
   if is_set(sys4.body_char[1],5) then
      begin
         writeln(f,'        Fanged Jaw, 2 EP');
         total_ep:=total_ep+2;
      end;
   if is_set(sys4.table_abil[2],7) then
      begin
         writeln(f,'        Poison Glands (Serious, Instant effect), 6 EP');
         total_ep:=total_ep+6;
      end;
   if is_set(sys4.table_abil[4],1) then
      begin
         writeln(f,'        Electroshock level 3, 9 EP');
         total_ep:=total_ep+9;
      end;
   if sys4.size_creat<40 then sys4.attrib[9]:=sys4.attrib[9]*2;  {due to scale factor}
   for a:= 1 to 10 do
      begin
         case a of
            1: tmp:=sys4.attrib[10];
            2: tmp:=sys4.attrib[2];
            3: begin
                  tmp:=trunc((sys4.attrib[2]+sys4.attrib[6]+sys4.attrib[14])/3);
                  if is_set(sys4.table_abil[7],7) then tmp:=tmp+2;
               end;
            4: begin
                  tmp:=trunc((sys4.attrib[10]+sys4.attrib[8]*2)/3);
                  if is_set(sys4.table_abil[10],4) then tmp:=tmp+2;
               end;
            5: tmp:=trunc((sys4.attrib[11]*2+sys4.attrib[10])/3);
            6: begin
                  tmp:=sys4.attrib[11]*2-sys4.attrib[9];
                  if is_set(sys4.table_abil[10],5) then tmp:=tmp+2;
               end;
            7: tmp:=trunc((sys4.attrib[9]*2+sys4.attrib[2])/3);
            8: tmp:=sys4.attrib[9];
            9: tmp:=trunc((sys4.attrib[9]*1.5+sys4.attrib[2]*1.5)/3);
            10: tmp:=sys4.attrib[11];
         end;
         case tmp of
            1..4  : result:=-2;
            5..7  : result:=-1;
            8..12 : result:=0;
            13..16: result:=+1;
            17..20: result:=+2;
            21..30: result:=+3;
            31..50: result:=+4;
            else    result:=-3;
         end;
         if result>0 then
            begin
                writeln(f,'        Enhanced ',fuzion_attribute[a],' +',result,' , ',
                   result*5,' EP');
                total_ep:=total_ep+result*5;
            end;
         if result<0 then
            begin
                writeln(f,'        Reduced ',fuzion_attribute[a],' ',result,' , ',
                   result*5,' EP');
                total_ep:=total_ep-result*5;
            end;
      end;
   if is_set(sys4.table_abil[11],6) then
          begin
              writeln(f,'        Enhanced +2 HIT, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[4],8) then
          begin
              writeln(f,'        Decelerated Time Scale, -20 EP');
              total_ep:=total_ep-20;
          end;
   if is_set(sys4.table_abil[3],8) then
          begin
              writeln(f,'        Full shapeshifting, 25 EP');
              total_ep:=total_ep+25;
          end;
   if (sys4.attrib[7]-12)>0 then
          begin
              writeln(f,'        Psionic powers (',(sys4.attrib[7]-12)*4,' PP), ',
                 (sys4.attrib[7]-12)*10,' EP');
              total_ep:=total_ep+(sys4.attrib[7]-12)*10;
          end;
   if is_set(sys4.table_abil[5],5) then
          begin
              tmp:=trunc((5-sys4.attrib[8])*1.5);
              if tmp<0 then tmp:=1;
              writeln(f,'        MAGE level (',tmp,') ',tmp*5,' EP');
              total_ep:=total_ep+tmp*5;
          end;

   {** New features v1.57c**}
   if is_set(sys4.table_abil[2],1) then
          begin
              writeln(f,'        Cold environment resistance level 1, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[2],4) then
          begin
              writeln(f,'        Heat environment resistance level 1, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[2],8) then
          begin
              writeln(f,'        Radiation environment resistance level 2, 4 EP');
              total_ep:=total_ep+4;
          end;
   if is_set(sys4.table_abil[9],7) then
          begin
              writeln(f,'        Illness environment resistance level 1, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[3],3) then
          begin
              writeln(f,'        Wall climbing, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[3],4) then
          begin
              writeln(f,'        Web spinning, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[3],5) then
          begin
              writeln(f,'        Nictating membrane, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[3],3) then
          begin
              writeln(f,'        Mimicry, 2 EP');
              total_ep:=total_ep+2;
          end;
   if is_set(sys4.table_abil[4],4) then
          begin
              writeln(f,'        Dampen level 2, 6 EP');
              total_ep:=total_ep+6;
          end;
   if is_set(sys4.table_abil[4],6) then
          begin
              writeln(f,'        Sonic beam level 2, 6 EP');
              total_ep:=total_ep+6;
          end;
   if is_set(sys4.table_abil[5],3) then
          begin
              writeln(f,'        Stretching, 3 EP');
              total_ep:=total_ep+3;
          end;


   writeln(f,'Racial Complications : ');
   complic:=false;
   case sys4.attrib[3] of
     1..4: begin
              writeln(f,'        Constant Severe Intolerance, -8 EP');
              total_ep:=total_ep-8;
              complic:=true;
           end;
     5..8: begin
              writeln(f,'        Infrequent Strong Intolerance, -6 EP');
              total_ep:=total_ep-6;
              complic:=true;
          end;
   end;
   if is_set(sys4.table_abil[10],8) then
          begin
              writeln(f,'        Uncommon addiction, -5 EP');
              total_ep:=total_ep-5;
              complic:=true;
          end;
   if is_set(sys4.table_abil[10],6) then
          begin
              writeln(f,'        Foul odor, -7 EP');
              total_ep:=total_ep-7;
              complic:=true;
          end;
   if is_set(sys4.table_abil[10],3) then
          begin
              writeln(f,'        Strange appearance, -8 EP');
              total_ep:=total_ep-8;
              complic:=true;
          end;
   if is_set(sys4.table_abil[9],3) then
          begin
              writeln(f,'        Water addiction, -2 EP');
              total_ep:=total_ep-2;
              complic:=true;
          end;
   if is_set(sys4.table_abil[8],5) then
          begin
              writeln(f,'        Disgusting habits, -5 EP');
              total_ep:=total_ep-5;
              complic:=true;
          end;
   {**New complications v1.57c}
   if is_set(sys4.table_abil[1],2) then
          begin
              writeln(f,'        Minor Reduced hearing, -2 EP');
              total_ep:=total_ep-2;
              complic:=true;
          end;
   if is_set(sys4.table_abil[1],5) then
          begin
              writeln(f,'        Major Reduced sight, -2 EP');
              total_ep:=total_ep-2;
              complic:=true;
          end;

   if not complic then writeln(f,'        None, racially');
   if sys4.size_creat<40 then
      begin
         total_ep:=trunc(total_ep/3);
         writeln(f,'Scale : x1/5 Mini-scale');
      end
     else writeln(f,'Scale : x1 Human-scale');
   writeln(f,'TOTAL POINT COST : ',total_ep,' EP');
   writeln(f,'OP COST : ',total_ep-75,' OP');
end;

procedure master_conversion (sys4:alien_record;output_form:byte;var f:text);
var   result:smallint;
      str_result,b1,b2:string;
      a,tmp:byte;
      change_stat:boolean;
const master_attribute: ARRAY [1..11] of STRING[2] =
       ('St','Qu','Pr','In','Em','Co','Ag','SD','Me','Re','Ap');
      master_res: ARRAY [1..5] of STRING[7] =
       ('Ess    ','Chan   ','Men/Tel','Poison ','Disease');
      master_heal:ARRAY [1..6] of STRING[5] =
       ('SlDp ','StDt ','RecMp','StLng','TyHtD','MxHts');
      master_ability: ARRAY [1..max_abilities] of STRING[50] =
       ('Acute hearing (special ability 6)','Poor hearing','Acute sense of smell (special ability 3)',
       'Acute vision','Poor vision','Ambidextrous','Chameleon skin','Cold sensitivity',
       'Cold tolerance','Color blind','Heat sensitivity','Heat tolerance',
       'Infrared vision','Natural night vision (special ability 4)','Poison','Radiation tolerance',
       '','Sonar','Wall climbing','Web spinning','Inner eyelid (special ability 1)',
       'Radio hearing','Acid secretion','Metamorphosis','Electric blast',
       'Hypnotism','Mimicry','Dampen','360 degrees vision','Sonic beam',
       'Vampirism','Slow motion','Sealed system','Clone','Stretching',
       '','Mystical power','Independent eyes','Quick maturity','Infertile','Hive mind',
       'Bicephalous','','Racial memory','Universal digestion','Pressure support',
       'Poloarized eyes','','Cultural adaptability','Field sense','Cold blooded biology',
       'Winged Flight','Gill system (special ability 11)','Flight',
       'Incredible appearance (special ability 7)','Spectrum vision','Ultrasonic hearing',
       'Microscopic vision','Blind','Deafness','Odious racial habit','Trading bonus skill',
       'Engineering bonus skill','Astronautic bonus skill','Weapons bonus skill',
       'Scientific bonus skill','Water dependency','Light sensitivity','Involuntary dampen',
       'Sound sensitivity','Disease tolerance','Memory mode (special ability 9)','',
       'No sense of smell/taste','Strange appearance','Manual dexterity',
       'Perfect balance','Foul odor','Skin color change','Dependency',
       'High fecundity','Cybernetic enhancements','Computer skill bonus',
       'Leap (special ability 8)','Vibration sense','Toughness','High gravity sensitivity','',
       'Extra Heart','Heavy sleeper','Light sleeper','Chemical communication',
       'Lightening calculator','Time sense','EM Imaging','Ultrasonic communication');
begin
   writeln(f);
   case output_form of
     0,1: begin
            b1:='';
            b2:='';
          end;
     2  : begin
            b1:='<center><b>';
            b2:='</b></center>';
          end;
   end;
   writeln(f,b1,' SpaceMaster conversion',b2);
   if output_form<>2 then writeln(f,' **********************');
   writeln(f);
   for a:=1 to 11 do
     begin
       write(f,master_attribute[a],' : ');
       case a of
         1: result:=sys4.attrib[9]-10;
         2: begin
              result:=sys4.attrib[11]-10;
              if is_set(sys4.table_abil[4],8) then result:=result-1;
            end;
         3: begin
              result:=sys4.attrib[2]+sys4.attrib[13]+sys4.attrib[14]-30;
              if is_set(sys4.table_abil[7],7) then result:=result+2;
              if is_set(sys4.table_abil[10],6) then result:=result-2;
            end;
         4: result:=sys4.attrib[7]+sys4.attrib[10]-20;
         5: result:=sys4.attrib[7]-10;
         6: begin
              result:=2*sys4.attrib[9]+sys4.attrib[2]-30;
              if is_set(sys4.table_abil[11],6) then result:=result+2;
            end;
         7: begin
              result:=sys4.attrib[11]-sys4.attrib[9];
              if is_set(sys4.table_abil[10],4) then result:=result+1;
              if is_set(sys4.table_abil[10],5) then result:=result+1;
            end;
         8: result:=sys4.attrib[2]-10;
         9: begin
              result:=sys4.attrib[11]+sys4.attrib[2]-20;
              if is_set(sys4.table_abil[9],8) then result:=result+2;
              if is_set(sys4.table_abil[6],4) then result:=result+2;    {Racial Memory}
            end;
         10: result:=sys4.attrib[11]+sys4.attrib[4]-20;
         11: begin
              result:=0;
              if is_set(sys4.table_abil[10],3) then result:=result-3;
              if is_set(sys4.table_abil[7],7) then result:=result+2;
              case sys4.app_genre of
                13: result:=result-2;
                15: result:=result-1;
              end;
              if sys4.attrib[3]>14 then result:=result+1;
             end;
       end;
       result:=result*5;
       if result<-70 then result:=-70;
       if result>50 then result:=50+trunc((result-50)/10)*5;
       if result<=0 then writeln(f,result:2) else writeln(f,'+',result);
     end;
   writeln(f);
   writeln(f,'Resistance rolls:');
   for a:=1 to 5 do
     begin
       write(f,'  ',master_res[a],': ');
       case a of
         1,2: begin
                result:=sys4.devotion-5;
                if is_set(sys4.table_abil[5],5) then result:=result+2;  {Mystical power}
              end;
         3  : result:=sys4.attrib[7]+sys4.attrib[2]-20;
         4  : begin
                result:=sys4.attrib[9]-10;
                if is_set(sys4.table_abil[2],7) then result:=result+3;
                if is_set(sys4.table_abil[5],4) then result:=result+2;
                if is_set(sys4.table_abil[6],3) then result:=result+1;
                if is_set(sys4.table_abil[11],8) then result:=result-2;
              end;
         5  : begin
                result:=sys4.attrib[9]-10;
                if is_set(sys4.table_abil[5],4) then result:=result+1;
                if is_set(sys4.table_abil[9],7) then result:=result+3;
                if is_set(sys4.table_abil[6],8) then result:=result-2;
              end;
       end;
       result:=result*5;
       if result<-70 then result:=-70;
       if result>50 then result:=50+trunc((result-50)/10)*5;
       if result<=0 then writeln(f,' ',result:2) else writeln(f,' +',result);
     end;
   writeln(f);
   writeln(f,'Healing rates:');
   for a:=1 to 6 do
     begin
       write(f,'  ',master_heal[a],' : ');
       case a of
          1: begin
               result:=4+sys4.devotion;
               if sys4.app_genre=18 then result:=result-6;            {Mecanoid}
               if result<2 then result:=2;
               writeln(f,result);
             end;
          2: begin
               result:=-trunc((sys4.attrib[7]-6)/5);
               if sys4.attrib[9]>13 then result:=result-1;
               if is_set(sys4.table_abil[11],6) then result:=result-1; {Toughness}
               if sys4.app_genre=18 then result:=result+5;             {Mecanoid}
               if is_set(sys4.table_abil[10],2) then result:=result+2; {Cyber enhancements}
               writeln(f,result);
             end;
          3: begin
               result:=10;
               if sys4.attrib[9]<8 then result:=7;
               if sys4.attrib[9]<5 then result:=5;
               if is_set(sys4.table_abil[3],1) then result:=result+5;  {Fast healing}
               if is_set(sys4.table_abil[6],3) then result:=result+5;  {Regeneration}
               writeln(f,result/10:0:1,'x');
             end;
          4: begin
               result:=trunc(sys4.attrib[10]/5)+1;
               if is_set(sys4.table_abil[10],1) then result:=result+3;  {Language talent}
               if is_set(sys4.table_abil[7],1) then result:=result+1;   {Cultural adaptab.}
               writeln(f,result);
             end;
          5: begin
               case sys4.attrib[9] of
                 1..5  : str_result:='D4';
                 6..8  : str_result:='D8';
                 9..11 : str_result:='D10';
                 12..14: str_result:='D10+1';
                 15..17: str_result:='D10+2';
                 18..20: str_result:='D20';
                 else str_result:='D20+2';
               end;
               writeln(f,str_result);
             end;
          6: begin
                result:=sys4.attrib[9]*10+10;
                writeln(f,result);
             end;
       end;
     end;
   writeln(f);
   writeln(f,'Special abilities:');
   change_stat:=false;
   for a:=1 to max_abilities do
        begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                 begin
                   if master_ability[a]<>'' then
		       begin
                       	    writeln(f,'  -',master_ability[a]);
			    change_stat:=true;
		       end;
                 end;
        end;
   str_result:='';
   case sys4.body_cover_type of
     2   : str_result:='(AT 4)';
     3   : str_result:='(AT 3)';
     5,10: str_result:='(AT 6)';
     6   : str_result:='(AT 5)';
     7,8 : str_result:='(AT 12)';
   end;
   if str_result<>'' then
      begin
        writeln(f,'  -Natural armor ',str_result);
        change_stat:=true;
      end;
   if not change_stat then writeln(f,'  -None');
   writeln(f);
end;

procedure alternity_conversion (sys4:alien_record;output_form:byte;var f:text);
var    result,high_stat,low_stat:smallint;
       a,tmp,misc_abil:byte;
       change_stat:boolean;
       b1,b2:string;
const alternity_stat: ARRAY [1..6] of string[3] =
       ('STR','DEX','CON','INT','WIL','PER');
      altern_ability: ARRAY [1..max_abilities] of STRING[30] =
       ('Enhanced hearing','Poor hearing','Enhanced smell','Enhanced vision',
       'Poor vision','Ambidextrous','Chameleon flesh','Cold sensitivity',
       'Cold tolerance','Color blind','Heat sensitivity','Heat tolerance',
       'Thermal vision','Night vision','Poison attack','Radiation tolerance',
       'Enhanced healing','Sonar','Wall climbing','Web spinning','Nictating membrane',
       'Radio hearing','Acid touch','Metamorphosis','Electric blast',
       'Hypnotism','Mimicry','Dampen','360 degrees vision','Sonic beam',
       'Vampirism','Slow motion','Sealed system','Clone','Stretching',
       'Systemic antidote','Mystical power','Independent eyes','Quick maturity',
       'Infertile','Hive mind','Bicephalous','Regeneration','Racial memory',
       'Universal digestion','Pressure support','Poloarized eyes',
       'Vulnerability to disease','Cultural adaptability','Field sense',
       'Cold blooded biology','Winged Flight','Water breathing','Flight',
       'Charisma','Spectrum vision','Ultrasonic hearing','Microscopic vision','Blind',
       'Deafness','Odious racial habit','Business skill bonus',
       'Technical skill bonus','Space vehicle skill bonus','One weapon skill bonus',
       'Physical science skill bonus','Water dependency','Light sensitivity','Involuntary dampen',
       'Sound sensitivity','Disease tolerance','Eidetic memory','Language talent',
       'No sense of smell/taste','Strange appearance','Manual dexterity',
       'Perfect balance','Foul odor','Skin color change','Dependency',
       'High fecundity','Cybernetic enhancements','Computer operation skill bonus',
       'Leap','Vibration sense','Superior durability','High gravity sensitivity',
       'Toxin intolerance','Extra Heart','Heavy sleeper','Light sleeper',
       'Chemical communication','Lightening calculator','Time sense','EM Imaging',
       'Ultrasonic communication');
      altern_misc_ability: ARRAY [1..7] of string[40] =
       ('Psionic powers I','Psionic powers II','No psionic powers',
        'Primitive culture','Natural weapon','Body armor',
        '5 skill points bonus');
begin
   writeln(f);
   case output_form of
     0,1: begin
            b1:='';
            b2:='';
          end;
     2  : begin
            b1:='<center><b>';
            b2:='</b></center>';
          end;
   end;
   writeln(f,b1,' Alternity conversion',b2);
   if output_form<>2 then writeln(f,' ********************');
   writeln(f);
   for a:=1 to 6  do
     begin
       write(f,alternity_stat[a],' : ');
       case a of
         1: result:=sys4.attrib[9];
         2: result:=sys4.attrib[11];
         3: result:=trunc((sys4.attrib[9]+Log10(sys4.mass/7)*10)/2);
         4: result:=sys4.attrib[10];
         5: result:=sys4.attrib[2];
         6: begin
              result:=sys4.attrib[3]-sys4.attrib[14]+10;
              if is_set(sys4.table_abil[7],7) then result:=result+2;
              if is_set(sys4.table_abil[9],2) then result:=result-2;
              if result<5 then result:=5;
            end;
       end;
       result:=trunc(result*0.8)+2;
       low_stat:=trunc(result-result/2);
       high_stat:=trunc(result+result/2);
       if low_stat<2 then low_stat:=2;
       if high_stat<low_stat then high_stat:=low_stat+1;
       if high_stat>18 then high_stat:=18;
       writeln(f,low_stat:2,'-',high_stat:2);
     end;
   writeln(f);
   writeln(f);
   { -- Special abilities -- }
   misc_abil:=0;
   if sys4.attrib[7]>16 then set_bit(misc_abil,1)
      else if sys4.attrib[7]>13 then set_bit(misc_abil,2)
         else if sys4.attrib[7]<6 then set_bit(misc_abil,3);
   if sys4.attrib[8]<4 then set_bit(misc_abil,4);
   if is_set(sys4.body_char[1],3) then set_bit(misc_abil,5);
   if is_set(sys4.body_char[1],5) then set_bit(misc_abil,5);
   if is_set(sys4.body_char[1],6) then set_bit(misc_abil,5);
   if is_set(sys4.body_char[1],7) then set_bit(misc_abil,5);
   case sys4.body_cover_type of
     2,5,7,8,10: set_bit(misc_abil,6);
   end;
   if (sys4.attrib[4]>14) and (sys4.attrib[2]>12) and (sys4.attrib[3]>11)
     and (sys4.attrib[10]>10) then set_bit(misc_abil,7);
   writeln(f,'Special abilities:');
   change_stat:=false;
   for a:=1 to max_abilities do
        begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                 begin
                   if gurps_ability_cost[a]<>0 then
		       begin
                       	    writeln(f,'  -',altern_ability[a]);
			    change_stat:=true;
		       end;
                 end;
        end;
   for a:=1 to 8 do
         if is_set(misc_abil,a) then
                 begin
         	    write(f,'  -',altern_misc_ability[a]);
		    change_stat:=true;
                    case a of
                       5: begin
                            if sys4.attrib[9]>12 then write(f,' d4+2s/d4w/d4+2w (LI/O)')
                               else write(f,' d4w/d4+2w/d4m (LI/O)');
                          end;
                       6: begin
                           if sys4.body_cover_type=2 then
                              write(f,' d4+1 (LI),d4 (LI), d4-1 (En)');
                           if sys4.body_cover_type=5 then
                              write(f,' d6+1 (LI),d4+1 (LI), d4 (En)');
                           if sys4.body_cover_type>5 then
                               write(f,' d6+2 (LI),d6+1 (LI), d6+1 (En)');
                          end;
                    end;
                    writeln(f);
                 end;
   if not change_stat then writeln(f,'  -None');
   writeln(f);
end;


procedure battle_conversion (sys4:alien_record;output_form:byte;var f:text);
   var result,charisma:smallint;
       change_stat:boolean;
       aux,b1,b2:string;
       a,arms_nbr,pseudo_nbr,dual_arms_nbr,tent_nbr,tmp,misc_abil:byte;
begin
   writeln(f);
   case output_form of
     0,1: begin
            b1:='';
            b2:='';
          end;
     2  : begin
            b1:='<center><b>';
            b2:='</b></center>';
          end;
   end;
   writeln(f,b1,' Battlelords of the 23rd century conversion',b2);
   if output_form<>2 then writeln(f,' ******************************************');
   writeln(f);
   writeln(f,'Vital statistics');
   dual_arms_nbr:=0;
   arms_nbr:=0;
   tent_nbr:=0;
   pseudo_nbr:=0;
   for a:=1 to sys4.limbs_number do
       begin
         case sys4.limbs_genre[a] of
           4: dual_arms_nbr:=dual_arms_nbr+1;
           5: arms_nbr:=arms_nbr+1;
           6: tent_nbr:=tent_nbr+1;
           7: pseudo_nbr:=pseudo_nbr+1;
         end;
       end;
   change_stat:=false;
   for a:=1 to 8 do
     begin
       result:=0;
       case a of
         1: result:=(sys4.attrib[9]-10)*5;
         2: begin
              result:=trunc(sys4.attrib[9]+sys4.attrib[11]-20*(Log10(sys4.mass/7)))*5;
              if sys4.limbs_number=0 then result:=-50
                 else if (arms_nbr+pseudo_nbr+dual_arms_nbr+tent_nbr)=0
                    then result:=-15;
              if is_set(sys4.body_char[1],7) then result:=result-10;
            end;
         3: result:=(sys4.attrib[10]-10)*5;
         4: result:=(sys4.attrib[11]-10)*5;
         5: result:=trunc(Log10(sys4.mass/70)*10)*5;
         6: result:=(sys4.attrib[1]-10)*5;
         7: result:=(sys4.attrib[10]+sys4.attrib[7]-20)*5;
         8: begin
              result:=(sys4.attrib[3]-10)*5;
              if is_set(sys4.table_abil[7],7) then result:=result+15;
              if is_set(sys4.table_abil[9],2) then result:=result-20;
              charisma:=result;
            end;
       end;
       if result<-30 then result:=-30
          else if result>40 then result:=result-10;
       if result>50 then result:=50;
       if result>0 then
            begin
             writeln(f,'  +',result:2,' ',battle_stat[a]);
             change_stat:=true;
            end
          else if result<0 then
            begin
             writeln(f,'  ',result:3,' ',battle_stat[a]);
             change_stat:=true;
            end;
     end;
   if not change_stat then writeln(f,'  No modifiers');
   writeln(f);
   writeln(f,'Secondary statistics');
   change_stat:=false;
   for a:=9 to 12 do
     begin
       result:=0;
       case a of
         9: result:=(sys4.attrib[15]-6)*5;
         10: result:=(sys4.attrib[1]*2+sys4.attrib[2]-30)*5;
         11: result:=trunc((charisma/5+(sys4.attrib[2]-10))/2)*5;
         12: result:=trunc((charisma/5+(sys4.attrib[14]-10))/2)*5;
       end;
       if result<-30 then result:=-30
              else if result>40 then result:=result-10;
       if result>50 then result:=50;
       if result>0 then
            begin
             writeln(f,'  +',result:2,' ',battle_stat[a]);
             change_stat:=true;
            end
          else if result<0 then
            begin
             writeln(f,'  ',result:3,' ',battle_stat[a]);
             change_stat:=true;
            end;
     end;
   if not change_stat then writeln(f,'  No modifiers');
   writeln(f);

   {Misc abilities}
   misc_abil:=0;
   if sys4.attrib[7]>13 then set_bit(misc_abil,1);
   if sys4.environment_type=3 then set_bit(misc_abil,2);
   if sys4.environment_type=4 then set_bit(misc_abil,3);
   if (sys4.body_cover_type=2) or (sys4.body_cover_type=2)
       then set_bit(misc_abil,4);
   if sys4.body_cover_type=7 then set_bit(misc_abil,5);
   if (sys4.body_cover_type=8) or (sys4.body_cover_type=10)
       then set_bit(misc_abil,6);
   if ((arms_nbr+tent_nbr)>1) and (round(sys4.attrib[11]/5)>1)
       then set_bit(misc_abil,7);

   writeln(f,'Special abilities');
   change_stat:=false;
   for a:=1 to max_abilities do
        begin
          tmp:=1+trunc((a-1)/8);
          if (is_set(sys4.table_abil[tmp],a-(tmp-1)*8)) and (a<>86) then
                 begin
                   if gurps_ability_cost[a]>0 then
		       begin
                       	    writeln(f,'  -',battle_ability[a]);
			    change_stat:=true;
		       end;
                 end;
        end;
   for a:=1 to 8 do
         if is_set(misc_abil,a) then
                 begin
         	    writeln(f,'  -',battle_misc_ability[a]);
		    change_stat:=true;
                 end;
   if not change_stat then writeln(f,'  -None');

   writeln(f);
   writeln(f,'Liabilities');
   change_stat:=false;
   for a:=1 to max_abilities do
        begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                 begin
                   if gurps_ability_cost[a]<0 then
		       begin
                       	    writeln(f,'  -',battle_ability[a]);
			    change_stat:=true;
		       end;
                 end;
        end;
   if not change_stat then writeln(f,'  -None');
   writeln(f);

   writeln(f,'Basic information');
   write(f,'  Body Points       : ');
   result:=sys4.attrib[9];
   if is_set(sys4.table_abil[11],6) then result:=result+1;
   case result of
     1,2  : aux:='d4';
     3,4  : aux:='1+d4';
     5,6  : aux:='2+d4';
     7,8  : aux:='1+d6';
     9..11: aux:='2+d6';
     12,13: aux:='4+d6';
     14   : aux:='6+d6';
     15   : aux:='5+d8';
     16   : aux:='8+d6';
     17   : aux:='7+d10';
     18   : aux:='7+d12';
     19,20: aux:='10+d12';
     21,22: aux:='12+d12';
     23,24: aux:='14+d12';
     else   aux:='16+d12';
   end;
   writeln(f,aux);
   write(f,'  Movement          : ',round(sys4.attrib[11]*4/5),'/');
   write(f,round(sys4.attrib[11]*sys4.attrib[9]*2/25),'/');
   writeln(f,round(sys4.attrib[11]*sys4.attrib[9]*sys4.attrib[9]*16/250));
   write(f,'  Number of attacks : ',round(sys4.attrib[11]/5));
   if is_set(sys4.body_char[1],5) then write(f,' +1 bite');
   if is_set(sys4.body_char[1],1) and (sys4.attrib[9]>13) then write(f,' +1 tail');
   writeln(f);
   write(f,'  Damage            : ');
   if sys4.attrib[9]>13 then writeln(f,'1-2') else writeln(f,'1');

   {  Vision  }
   write(f,'  Vision            : ');
   result:=0;
   if is_set(sys4.table_abil[1],4) then result:=result+50;
   if is_set(sys4.table_abil[1],5) then result:=result-30;
   if is_set(sys4.table_abil[1],2) then result:=result+5;
   if is_set(sys4.table_abil[10],2) then result:=result+5;
   if is_set(sys4.table_abil[2],2) then result:=result-10;
   if is_set(sys4.table_abil[4],5) then result:=result+10;
   if is_set(sys4.table_abil[3],2) then result:=result-10;
   if is_set(sys4.table_abil[7],6) then result:=result+10;
   if is_set(sys4.table_abil[8],3) then result:=result-70;
   case sys4.environment_type of
     3        : result:=result-5;
     4        : result:=result-10;
     5        : result:=result+5;
   end;
   if result>-1 then writeln(f,'+',result:2,'%') else writeln(f,result:3,'%');

   {  Smell  }
   write(f,'  Smell             : ');
   result:=0;
   if is_set(sys4.table_abil[1],3) then result:=result+50;
   if is_set(sys4.table_abil[10],2) then result:=result-30;
   if is_set(sys4.table_abil[10],6) then result:=result-5;
   case sys4.app_genre of
     4,5,22,23: result:=result+20;
     7,12     : result:=result+5;
     8,9,15,17: result:=result-5;
   end;
   case sys4.environment_type of
     3        : result:=result-5;
     4        : result:=result-10;
   end;
   if result>-1 then writeln(f,'+',result:2,'%') else writeln(f,result:3,'%');

   {  Hearing  }
   write(f,'  Hearing           : ');
   result:=0;
   if is_set(sys4.table_abil[1],1) then result:=result+50;
   if is_set(sys4.table_abil[1],2) then result:=result-10;
   if is_set(sys4.table_abil[1],5) then result:=result+5;
   if is_set(sys4.table_abil[3],2) then result:=result+10;
   if is_set(sys4.table_abil[4],3) then result:=result+10;
   if is_set(sys4.table_abil[8],1) then result:=result+30;
   if is_set(sys4.table_abil[8],4) then result:=result-50;
   if is_set(sys4.table_abil[9],6) then result:=result-5;
   case sys4.app_genre of
     4,5    : result:=result+10;
     12     : result:=result+5;
   end;
   if result>-1 then writeln(f,'+',result:2,'%') else writeln(f,result:3,'%');

   {  Survival Matrix Roll }
   writeln(f,'  SMR               : ');
   for a:=1 to 10 do
      begin
         if a=4 then result:=sys4.attrib[7] else result:=sys4.attrib[9];
         case a of
           1   : begin
                  if is_set(sys4.table_abil[5],4) then result:=result+10;
                  if is_set(sys4.table_abil[5],1) then result:=result+15;
                  if is_set(sys4.table_abil[6],8) then result:=result-2;
                 end;
           2   : if is_set(sys4.table_abil[2],8) then result:=result+20;
           3   : begin
                    if is_set(sys4.table_abil[5],4) then result:=result+5;
                    if is_set(sys4.table_abil[9],7) then result:=result+20;
                    if is_set(sys4.table_abil[11],6) then result:=result-2;
                    if is_set(sys4.table_abil[6],8) then result:=result-7;
                 end;
           5   : begin
                    if is_set(sys4.table_abil[2],7) then result:=result+5;
                    if is_set(sys4.table_abil[2],7) then result:=result+5;
                    if is_set(sys4.table_abil[11],6) then result:=result-5;
                 end;
           6   : begin
                    if is_set(sys4.table_abil[1],2) then result:=result+10;
                    if is_set(sys4.table_abil[8],4) then result:=result+20;
                    if is_set(sys4.table_abil[9],6) then result:=result-8;
                    if is_set(sys4.table_abil[8],1) then result:=result-3;
                    if is_set(sys4.table_abil[1],1) then result:=result-2;
                 end;
           7   : begin
                    if is_set(sys4.table_abil[4],1) then result:=result+5;
                    if sys4.body_cover_type=10 then result:=result-7;
                    if sys4.app_genre=17 then result:=result+10;
                 end;
           8   : begin
                    if is_set(sys4.table_abil[2],3) then result:=result-5;
                    if is_set(sys4.table_abil[2],4) then result:=result+5;
                    if sys4.body_cover_type=7 then result:=result+3;
                    if sys4.body_cover_type=8 then result:=result+7;
                    if sys4.app_genre=8 then result:=result-3;
                 end;
           9   : begin
                    if is_set(sys4.table_abil[3],7) then result:=result+8;
                    if sys4.body_cover_type=8 then result:=result+5;
                 end;
           10  : begin
                    if is_set(sys4.table_abil[1],8) then result:=result-8;
                    if is_set(sys4.table_abil[2],1) then result:=result+5;
                    if is_set(sys4.table_abil[7],3) then result:=result-5;
                    if sys4.body_cover_type=3 then result:=result+3;
                    if sys4.app_genre=9 then result:=result+2;
                 end;
         end;
         result:=trunc(result*battle_smr_cst[a]/10);
         if result<5 then result:=5
            else if result>99 then result:=99;
         case a of
           2..4,7..9: write(f,battle_smr[a],':',result:2,' ');
           1,6      : write(f,'    ',battle_smr[a],':',result:2,' ');
           5,10     : writeln(f,battle_smr[a],':',result:2,' ');
         end;
      end;

   writeln(f);
   writeln(f);
end;


procedure gurps_conversion (sys4:alien_record;output_form:byte;var f:text);
   var a,tmp,wings_nbr,arms_nbr,legs_nbr,dual_arms_nbr,tent_nbr:byte;
       cost,point_cost,temp_cost,result:smallint;
       ability:boolean;
       misc_adv,misc_dis:table_bool3;
       b1,b2:string;
begin
   writeln(f);
   case output_form of
     0,1: begin
            b1:='';
            b2:='';
          end;
     2  : begin
            b1:='<center><b>';
            b2:='</b></center>';
          end;
   end;
   writeln(f,b1,' Gurps conversion',b2);
   if output_form<>2 then writeln(f,' ****************');
   writeln(f);
   cost:=0;
   for a:=1 to 4 do
     begin
       write(f,gurps_stat[a],' : ');
       case a of
         1: result:=sys4.attrib[9]-10;
         2: begin
             result:=sys4.attrib[11]-10;
             if (sys4.size_creat/sys4.mass)>1 then result:=result+1
               else result:=result-1;
            end;
         3: result:=sys4.attrib[10]-10;
         4: result:=trunc(sys4.attrib[9]+Log10(sys4.mass/7)*10-20);
       end;
       if result<-9 then result:=-9;
       if result>-1 then write(f,'+',result) else write(f,result);
       if result<11 then temp_cost:=gurps_attribute_cost[result+10] else
          temp_cost:=gurps_attribute_cost[20]+25*(result-10);
       writeln(f,' (',temp_cost,' points)');
       cost:=cost+temp_cost;
     end;
   writeln(f);

   {--advantages--}
   writeln(f,'Advantages:');
   ability:=false;
   for a:=1 to max_abilities do
     begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                 begin
                   if gurps_ability_cost[a]>0 then
		       begin
                       	    writeln(f,'  -',gurps_special_ability[a],' (',
                               gurps_ability_cost[a],' points)');
			    ability:=true;
                            cost:=cost+gurps_ability_cost[a];
		       end;
                 end;
     end;
   misc_adv[1]:=0;
   misc_adv[2]:=0;
   if sys4.environment_type=3 then set_bit(misc_adv[1],1);
   if (sys4.body_cover_type=2) or (sys4.body_cover_type=5) then set_bit(misc_adv[1],2);
   if sys4.body_cover_type=8 then set_bit(misc_adv[1],3);
   if is_set(sys4.body_char[1],6) then set_bit(misc_adv[1],4);
   if sys4.attrib[12]>30 then set_bit(misc_adv[1],5);
   if sys4.attrib[11]>14 then set_bit(misc_adv[1],6);
   if sys4.attrib[2]>14 then set_bit(misc_adv[1],7);
   if (sys4.body_cover_type=7) or (sys4.body_cover_type=10) then set_bit(misc_adv[1],8);
   wings_nbr:=0;
   legs_nbr:=0;
   dual_arms_nbr:=0;
   arms_nbr:=0;
   tent_nbr:=0;
   for a:=1 to sys4.limbs_number do
       begin
         case sys4.limbs_genre[a] of
           1: wings_nbr:=wings_nbr+1;
           3: legs_nbr:=legs_nbr+1;
           4: dual_arms_nbr:=dual_arms_nbr+1;
           5: arms_nbr:=arms_nbr+1;
           6: tent_nbr:=tent_nbr+1;
         end;
       end;
   if dual_arms_nbr>0 then set_bit(misc_adv[2],1);
   if arms_nbr>1 then set_bit(misc_adv[2],4);
   if legs_nbr>1 then set_bit(misc_adv[2],5);
   if (tent_nbr>0) and ((arms_nbr+tent_nbr)>1) then set_bit(misc_adv[2],6);
   if is_set(sys4.body_char[1],7) then set_bit(misc_adv[2],2);
   if sys4.attrib[8]>7 then set_bit(misc_adv[2],3);
   if sys4.attrib[7]>13 then set_bit(misc_adv[2],7);
   if is_set(sys4.body_char[1],1) then set_bit(misc_adv[2],8);
   for a:=1 to 16 do
     begin
          tmp:=1+trunc((a-1)/8);
          if is_set(misc_adv[tmp],a-(tmp-1)*8) then
                 begin
                    point_cost:=gurps_ability_cost[a+max_abilities];
                    case a of
                       12: point_cost:=(arms_nbr-1)*gurps_ability_cost[a+max_abilities];
                       13: if legs_nbr=2 then point_cost:=5
                             else if legs_nbr=3 then point_cost:=10
                                 else if legs_nbr=4 then point_cost:=15;
                       15: if sys4.attrib[7]<17 then point_cost:=20
                              else if sys4.attrib[7]<20 then point_cost:=50
                                 else point_cost:=100;
                    end;
              	    writeln(f,'  -',gurps_misc_adv[a],' (',
                       point_cost,' points)');
		    ability:=true;
                    cost:=cost+point_cost;
                 end;
     end;
   if not ability then writeln(f,'  -None');


   {--disadvantages--}
   writeln(f,'Disadvantages:');
   ability:=false;
   misc_dis[1]:=0;
   misc_dis[2]:=0;
   for a:=1 to max_abilities do
     begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                 begin
                   if gurps_ability_cost[a]<0 then
		       begin
                       	    writeln(f,'  -',special_ability[a],' (',
                               gurps_ability_cost[a],' points)');
			    ability:=true;
                            cost:=cost+gurps_ability_cost[a];
		       end;
                 end;
     end;
   if sys4.environment_type=4 then set_bit(misc_dis[1],1);
   if  (dual_arms_nbr+arms_nbr)=0 then set_bit(misc_dis[1],2)
      else if is_set(sys4.body_char[1],7) then set_bit(misc_dis[2],1);
   if (sys4.attrib[11]>0) and (sys4.attrib[11]<5) then set_bit(misc_dis[1],3);
   if sys4.attrib[11]=0 then set_bit(misc_dis[1],4);
   if sys4.attrib[12]<7 then set_bit(misc_dis[1],5);
   if sys4.attrib[2]<5 then set_bit(misc_dis[1],6);
   if sys4.attrib[3]<5 then set_bit(misc_dis[1],7);
   if sys4.attrib[1]<5 then set_bit(misc_dis[1],8);
   if sys4.attrib[8]<7 then set_bit(misc_dis[2],2);
   if (sys4.attrib[5]+sys4.attrib[2])>30 then set_bit(misc_dis[2],3);
   if sys4.attrib[5]>15 then set_bit(misc_dis[2],4);
   if sys4.attrib[1]>16 then set_bit(misc_dis[2],5);
   if (sys4.attrib[2]+sys4.attrib[4])<11 then set_bit(misc_dis[2],6);
   for a:=1 to 16 do
     begin
          tmp:=1+trunc((a-1)/8);
          if is_set(misc_dis[tmp],a-(tmp-1)*8) then
                 begin
                    point_cost:=gurps_ability_cost[a+max_abilities+16];
                    case a of
                      10: begin
                           if sys4.attrib[8]=1 then point_cost:=-35;
                           if sys4.attrib[8]=2 then point_cost:=-30;
                           if sys4.attrib[8]=3 then point_cost:=-20;
                           if sys4.attrib[8]=4 then point_cost:=-15;
                           if sys4.attrib[8]=5 then point_cost:=-10;
                           if sys4.attrib[8]=6 then point_cost:=-5;
                          end;
                    end;
              	    writeln(f,'  -',gurps_misc_disadv[a],' (',
                       point_cost,' points)');
		    ability:=true;
                    cost:=cost+point_cost;
                 end;
     end;
   if not ability then writeln(f,'  -None');

   writeln(f);
   writeln(f,'It costs ',cost,' points to play this race');

end;

end.
