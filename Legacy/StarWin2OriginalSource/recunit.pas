UNIT RECUNIT;



INTERFACE

uses Classes;


TYPE string50 = string[50];
     table=array[1..20] of string;
     table_num=array[1..5] of byte;
     table_single=array[1..18] of single;
     table_0=array[1..2] of smallint;
     table_1=array[1..3] of byte;
     table_2=array[1..3] of single;
     table_3=array[1..3] of longint;
     table_5=array[1..18,1..3] of smallint;
     table_6=array[1..4] of longint;
     table_10=array[1..10] of string;
     table_13=array[1..13] of longint;
     table_20=array[1..5] of string;
     table_30=array[1..30] of string;
     table_40=array[1..40] of string;
     table_index=array[1..4] of single;
     table_byte=array[1..12] of byte;
     table_byte2=array[1..15] of byte;
     table_50=array[1..50] of string;
     table_bool=array[1..5] of byte;
     table_bool2=array[1..12] of byte;
     table_bool3=array[1..2] of byte;

     star_record = record
                     spe_class,type_spec,planet_nbr: table_1;
                     luminosity,mass_star: table_2;
                     age  : byte;
                     companion_dist: table_0;
                     astral_orbit: table_5;
                     star_nbr       : byte;
                     pln_id : table_3;
                     posX,posY,posZ : smallint;
                     allegiance : word;
                     code: byte; {0 Green 1 Red}
                     name: string[20];
                     misc_char : byte; {Not used}
                    end;
     planet_record= record
                     sun_id: longint; {-1 temporary record, used only by Unit6.pas}
                     alien_id : integer;
                     moon_id: longint;
                     allegiance: word;
                     atmos_type,world_type,water_type:byte;
                     atmos,smwr,axial_tilt,inclination:byte;
                     temp_avg:smallint;
                     satellites :byte;
                     hydrography :table_num;
                     rotation_period:longint;
                     diametre:longint;
                     density:byte;
                     pressure:single;
                     eccentricity:word;
                     orbit_radius: smallint;
                     mine_ress: table_num;
                     misc_charac: byte;  { 1 icy_core, 2 resonance, 3 water_tainted
                                           4 colony}
                     unusual: table_bool;
                     name: string[20];
                    end;
     moon_record=   record
                     pln_id : longint;
                     atmos_type,world_type,water_type:byte;
                     atmos :byte;
                     temp_avg :smallint;
                     hydrography :table_num;
                     diametre:longint;
                     density:byte;
                     pressure: single;
                     sat_orbit: word;
                     mine_ress: table_num;
                     misc_charac: byte;
                     name: string[20];
                    end;
     alien_record= record
                     pln_id: longint;
                     environment_type,body_type,limbs_number,diet_genre,
                     repro_genre,repro_meth_genre,gov_type,body_cover_type,
                     app_genre:byte;
                     mass,size_creat:smallint;
                     limbs_genre:table_byte;
                     attrib:table_byte2;
                     table_abil:table_bool2;
                     color_genre,hair_color,body_char,eye_color,eye_char:table_bool3;
                     hair_char,religion,devotion:byte;
                     name: string[20];
                   end;
     names_record   = record
                        body_type:byte;
                        body_Id:longint;
                        name:string50;
                      end;
     colony_record  = record
                        world_id: longint;
                        race,allegiance:word;
                        body_type,pop,col_class,crime,law,stability,
                         age,starport,gov: byte;
                        gnp,power: word;
                        pop_comp: byte;
                        misc_char: table_bool3;
                        eco_export,eco_import: byte;
                      end;
     contact_record = record
                        empire1,empire2: word;
                        relation,age: byte;
                      end;
     empire_record  = record
                        attrib: table_13;
                      end;

     planet_record_file  = file of planet_record;
     moon_record_file    = file of moon_record;
     alien_record_file   = file of alien_record;
     star_record_file    = file of star_record;
     name_record_file    = file of names_record;
     colony_record_file  = file of colony_record;
     contact_record_file = file of contact_record;
     empire_record_file  = file of empire_record;


     FUNCTION get_bv(bit:byte): byte;
     FUNCTION is_set(flag,bit:byte): boolean;
     PROCEDURE set_bit(var flag:byte;bit:byte);
     PROCEDURE unset_bit(var flag:byte;bit:byte);



IMPLEMENTATION


  function get_bv(bit:byte): byte;
     begin
         case bit of
              1: get_bv:=1;
              2: get_bv:=2;
              3: get_bv:=4;
              4: get_bv:=8;
              5: get_bv:=16;
              6: get_bv:=32;
              7: get_bv:=64;
              8: get_bv:=128;
              else writeln('Error in bit value  bv=',bit);
         end;
     end;


  function is_set(flag,bit:byte): boolean;
     var bv:byte;
     begin
          bv:=get_bv(bit);
          if (flag AND bv)=bv then is_set:=true else is_set:=false;
     end;


  procedure set_bit(var flag:byte;bit:byte);
     var bv:byte;
     begin
          bv:=get_bv(bit);
          flag:=flag OR bv;
     end;


  procedure unset_bit(var flag:byte;bit:byte);
     var bv:byte;
     begin
          bv:=get_bv(bit);
          flag:=flag AND (NOT bv);
     end;


END.