{Civilization Generator V1.6  (c) Aina Rasolomalala 03-2001

   v0.1 : first version
   v1.6 : CivGen is remplacing Empire with new stuffs

}

unit civgen1;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
   recunit, StdCtrls, ComCtrls;

type
  TForm1 = class(TForm)
    OpenDialog1: TOpenDialog;
    Button1: TButton;
    Button2: TButton;
    Button3: TButton;
    Button5: TButton;
    Button6: TButton;
    Label1: TLabel;
    Edit1: TEdit;
    CheckBox1: TCheckBox;
    Edit2: TEdit;
    Edit3: TEdit;
    Edit4: TEdit;
    Edit5: TEdit;
    Edit6: TEdit;
    Label2: TLabel;
    Label3: TLabel;
    Label4: TLabel;
    Label5: TLabel;
    Label6: TLabel;
    Label7: TLabel;
    Label8: TLabel;
    Edit7: TEdit;
    CheckBox2: TCheckBox;
    CheckBox3: TCheckBox;
    CheckBox4: TCheckBox;
    Label9: TLabel;
    Edit8: TEdit;
    Button4: TButton;
    CheckBox5: TCheckBox;
    Label10: TLabel;
    Edit9: TEdit;
    Label11: TLabel;
    Edit10: TEdit;
    StatusBar1: TStatusBar;
    CheckBox6: TCheckBox;
    CheckBox7: TCheckBox;
    CheckBox8: TCheckBox;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure Button3Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button4Click(Sender: TObject);

  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  Form1: TForm1;

implementation

uses civgen2,starunit,civgen3,Math;

{$R *.DFM}
var   namefile: string;
      dist_table: array[1..5] of word {= (5,20,50,100,300)};
      victory_value: single;
      mi_power,homeplanet,max_dist: array[0..65535] of longint;
      RP: array[0..65535] of smallint;
      alliance:array[0..65535] of boolean;
      tladvance,rp_diplo:array[0..65535] of byte;
      habit_table: Variant;
      diplo_table,file_table: Variant;
      economic_power,total_pop,trade_bonus: Variant;
      captive_pop,subject_pop,indep_col,indep_pop:Variant;
      colony_nbr,captive_world_nbr,subj_world,moon_nbr,subj_moon: Variant;
      mibonus: integer;
      max_range: byte;
      civ_factor: boolean;

const habitability: array [1..23] of shortint=
        (0,2,1,-1,1,0,0,7,8,8,8,8,1,2,5,1,2,2,1,1,0,0,7);
      evm : ARRAY [1..7] of byte =
       (1,5,10,30,70,150,200);
      mdm : ARRAY [1..10] of single =
       (0,0.1,0.25,0.5,0.75,1,1.3,1.6,1.9,2.2);


FUNCTION  de(face,nombre:word):word;far; external 'Stardll.dll';
PROCEDURE stardll_ini;far; external 'Stardll.dll';
procedure update_data(namefile:string;old_ver:byte);far;external 'Stardll.dll';
FUNCTION Ice_fraction (hydrosphere:byte; surface_temp : smallint) : byte;far;
         external 'Stardll.dll';

{** v1.6 Copy of the main_colony proc in order to show program progress **}

procedure main_colony_emp(outstr:string;var cyfile:colony_record_file;
    var ctfile:contact_record_file;var afile:alien_record_file);
var sys4: alien_record;
    sys7: colony_record;
    sys8: contact_record;
    a,b,fsize: longint;
    multiplier_pop:single;
    factor_pop,colony_type,prestige:byte;
    econ_value: ARRAY [1..2] of longint;
const pop_index: ARRAY [1..7] of single=
   (0.01,0.1,1,10,100,1000,10000);
begin
 reset(cyfile);
 reset(ctfile);
 reset(afile);

 fsize:=filesize(afile);
 economic_power:=VarArrayCreate([0,fsize],VarInteger);
 total_pop:=VarArrayCreate([0,fsize],VarInteger);
 trade_bonus:=VarArrayCreate([0,fsize],VarInteger);
 captive_pop:=VarArrayCreate([0,fsize],VarInteger);
 indep_pop:=VarArrayCreate([0,fsize],VarInteger);
 subject_pop:=VarArrayCreate([0,fsize],VarInteger);
 colony_nbr:=VarArrayCreate([0,fsize],VarInteger);
 captive_world_nbr:=VarArrayCreate([0,fsize],VarInteger);
 subj_world:=VarArrayCreate([0,fsize],VarInteger);
 moon_nbr:=VarArrayCreate([0,fsize],VarInteger);
 subj_moon:=VarArrayCreate([0,fsize],VarInteger);
 indep_col:=VarArrayCreate([0,fsize],VarInteger);

 for a:=0 to (filesize(afile)-1) do
    begin
       Application.ProcessMessages;
       economic_power[a]:=0;
       colony_nbr[a]:=0;
       captive_world_nbr[a]:=0;
       moon_nbr[a]:=0;
       total_pop[a]:=0;
       subj_world[a]:=0;
       captive_pop[a]:=0;
       subj_moon[a]:=0;
       subject_pop[a]:=0;
       indep_col[a]:=0;
       indep_pop[a]:=0;
       trade_bonus[a]:=0;
    end;
 for b:=0 to (filesize(cyfile)-1) do
    begin
       Form1.StatusBar1.SimpleText:='Collecting Infos: Colony '+IntToStr(b);
       seek(cyfile,b);
       read(cyfile,sys7);
       if (sys7.allegiance<>sys7.race) and (sys7.allegiance<>65535) then
           begin
             if sys7.body_type=1 then
                   captive_world_nbr[sys7.race]:=captive_world_nbr[sys7.race]+1
                else subj_moon[sys7.allegiance]:=subj_moon[sys7.allegiance]+1;
             factor_pop:=trunc(sys7.pop/10);
             multiplier_pop:=sys7.pop-factor_pop*10;
             if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
             captive_pop[sys7.race]:=captive_pop[sys7.race]+round((multiplier_pop+1)*
                  pop_index[factor_pop+1]);
           end;
       if  sys7.allegiance=65535 then
           begin
              indep_col[sys7.race]:=indep_col[sys7.race]+1;
              factor_pop:=trunc(sys7.pop/10);
              multiplier_pop:=sys7.pop-factor_pop*10;
              if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
              indep_pop[sys7.race]:=indep_pop[sys7.race]+round((multiplier_pop+1)*
                  pop_index[factor_pop+1]);
           end
         else
           begin
              economic_power[sys7.allegiance]:=economic_power[sys7.allegiance]+sys7.gnp;
              case sys7.body_type of
                 1: if sys7.race=sys7.allegiance then
                           colony_nbr[sys7.race]:=colony_nbr[sys7.race]+1
                       else subj_world[sys7.allegiance]:=subj_world[sys7.allegiance]+1;
                 2: moon_nbr[sys7.allegiance]:=moon_nbr[sys7.allegiance]+1;
              end;
              factor_pop:=trunc(sys7.pop/10);
              multiplier_pop:=sys7.pop-factor_pop*10;
              if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
              if sys7.race=sys7.allegiance then total_pop[sys7.race]:=total_pop[sys7.race]
                    +round((multiplier_pop+1)*pop_index[factor_pop+1])
                 else subject_pop[sys7.allegiance]:=subject_pop[sys7.allegiance]
                    +round((multiplier_pop+1)*pop_index[factor_pop+1]);
           end;
    end;
 seek(ctfile,0);
 for b:=0 to (filesize(ctfile)-1) do
    begin
       read(ctfile,sys8);
       if sys8.relation>2 then
          begin
             econ_value[1]:=economic_power[sys8.empire1];
             econ_value[2]:=economic_power[sys8.empire2];
             if econ_value[1]>econ_value[2] then econ_value[1]:=econ_value[2];
             trade_bonus[sys8.empire1]:=trade_bonus[sys8.empire1]+trunc(0.1*econ_value[1]);
             trade_bonus[sys8.empire2]:=trade_bonus[sys8.empire2]+trunc(0.1*econ_value[1]);
          end;
    end;

 closefile(cyfile);
 closefile(ctfile);
 closefile(afile);
end;



function Empire_power (sys4:alien_record; dist_factor:single): single;
var aux:single;
begin
  aux:=(6*sys4.attrib[1]+3*sys4.attrib[2]+sys4.attrib[5]+sys4.attrib[6]
    -sys4.attrib[3])/10+6*sys4.attrib[8];
  if is_set(sys4.table_abil[9],1) then dist_factor:=dist_factor+0.1;
  if dist_factor<0.5 then dist_factor:=0.5;
  Empire_power:=aux*dist_factor;
end;


{** v1.6 Major Imports and Exports procedure **}
procedure colony_eco(var sys7: colony_record);
begin
  case sys7.col_class of
     1: begin
          case de(10,1) of
            1..3: sys7.eco_export:=1;
            4..7: sys7.eco_export:=4;
            8,9 : sys7.eco_export:=8;
            10  : sys7.eco_export:=16;
          end;
          case de(10,1) of
            1..7: sys7.eco_import:=de(3,1)+8;
            8,9 : sys7.eco_import:=5;
            10  : sys7.eco_import:=2;
          end;
        end;
     2,7: begin
           case de(10,1) of
            1..4: sys7.eco_export:=de(5,1)+6;
            5..9: sys7.eco_export:=de(4,1)+11;
            10  : sys7.eco_export:=17;
           end;
           case de(10,1) of
            1..4: sys7.eco_import:=4;
            5   : sys7.eco_import:=5;
            6..7: sys7.eco_import:=de(5,1)+6;
            8   : sys7.eco_import:=6;
            9   : sys7.eco_import:=de(4,1)+11;
            10  : sys7.eco_import:=de(3,1)+15;
           end;
         end;
     3: begin
          case de(10,1) of
            1..2: sys7.eco_export:=6;
            3..8: sys7.eco_export:=de(5,1)+6;
            9   : sys7.eco_export:=14;
            10  : sys7.eco_export:=17;
          end;
          case de(10,1) of
            1,2 : sys7.eco_import:=4;
            3..7: sys7.eco_import:=2;
            8   : sys7.eco_import:=3;
            9   : sys7.eco_import:=5;
            10  : sys7.eco_import:=de(2,1)+9;
          end;
        end;
     4: begin
          case de(10,1) of
            1..5: sys7.eco_export:=2;
            6..9: sys7.eco_export:=5;
            10  : sys7.eco_export:=16;
          end;
          case de(10,1) of
            1..5: sys7.eco_import:=4;
            6..10: sys7.eco_import:=de(5,1)+6;
          end;
        end;
     5: begin
          case de(10,1) of
            1..5: sys7.eco_export:=3;
            6..10: sys7.eco_export:=6;
          end;
          case de(10,1) of
            1..5: sys7.eco_import:=4;
            6..10: sys7.eco_import:=de(5,1)+6;
          end;
        end;
     6: begin
          case de(10,1) of
            1..4: sys7.eco_export:=de(2,1)+11;
            5   : sys7.eco_export:=de(3,1)+15;
            6..10: sys7.eco_export:=0;
          end;
          case de(10,1) of
            1..5: sys7.eco_import:=4;
            6..10: sys7.eco_import:=de(5,1)+6;
          end;
        end;
     8: begin
          if sys7.starport=4 then
               case de(10,1) of
                  1..3: sys7.eco_export:=1;
                  4..7: sys7.eco_export:=4;
                  8,9 : sys7.eco_export:=8;
                  10  : sys7.eco_export:=16;
              end
             else
                case de(10,1) of
                  1..2: sys7.eco_export:=6;
                  3..8: sys7.eco_export:=de(5,1)+6;
                  9   : sys7.eco_export:=14;
                  10  : sys7.eco_export:=17;
               end;
          case de(10,1) of
            1..7: sys7.eco_import:=de(3,1)+8;
            8,9 : sys7.eco_import:=5;
            10  : sys7.eco_import:=2;
          end;
        end;
     9: begin
          case de(10,1) of
            1..9: sys7.eco_export:=0;
            10  : sys7.eco_export:=de(3,1)+15;
          end;
          case de(10,1) of
            1,2 : sys7.eco_import:=1;
            3..5: sys7.eco_import:=4;
            6   : sys7.eco_import:=6;
            7..10: sys7.eco_import:=de(4,1)+7;
          end;
        end;
     10: begin
          case de(10,1) of
            1,2 : sys7.eco_import:=4;
            3,4 : sys7.eco_import:=6;
            5..10: sys7.eco_import:=0;
          end;
        end;
     11: begin
          case de(10,1) of
            1   : sys7.eco_export:=14;
            2,3 : sys7.eco_export:=15;
            4   : sys7.eco_export:=17;
            5..10: sys7.eco_export:=0;
          end;
          case de(10,1) of
            1,2 : sys7.eco_import:=4;
            3,4 : sys7.eco_import:=6;
            5..10: sys7.eco_import:=0;
          end;
        end;
  end;
  if sys7.eco_import=sys7.eco_export then sys7.eco_export:=0;
end;


procedure colonize_proc(sys4:alien_record;pass:byte;var sys7: colony_record;
            mine_ress:table_num);
var   aux:shortint;
      rui,a:byte;
begin
  aux:=de(7,1)+1;
  if sys4.attrib[8]>8 then aux:=aux+1
     else if sys4.attrib[8]=6 then aux:=aux-2
         else if sys4.attrib[8]<6 then aux:=aux-15;
  if pass>10 then aux:=aux+1
     else if pass<2 then aux:=aux-1;
  if sys7.pop<40 then aux:=aux-1
     else if sys7.pop>49 then aux:=aux+1;
  case aux of
    1,2    : sys7.starport:=4;
    3..6   : sys7.starport:=3;
    7,8    : sys7.starport:=2;
    9..11  : sys7.starport:=1;
    else begin
           if sys4.attrib[8]>5 then sys7.starport:=4
              else sys7.starport:=5;
         end;
  end;
  case sys4.gov_type of
    1 : sys7.law:=1;
    21: sys7.law:=2;
    3,4,13: sys7.law:=de(5,1)+1;
    12: sys7.law:=de(5,1)+5;
    else sys7.law:=de(9,1)+1;
  end;
  aux:=de(8,1)+trunc((sys4.attrib[5]+sys4.attrib[6]-20)/4);
  if pass>10 then aux:=aux+1
     else aux:=aux-1;
  if aux<1 then aux:=1 else if aux>10 then aux:=10;
  sys7.stability:=aux;
  aux:=de(8,1)+2-trunc(sys7.law/3)-trunc(sys7.stability/3);
  if aux<1 then aux:=1;
  sys7.crime:=aux;
  case sys7.col_class of
    1: rui:=de(7,1)+5;
    4: begin
         rui:=de(5,1);
         if mine_ress[1]>40 then rui:=rui+3
            else if mine_ress[1]>60 then rui:=rui+5
                else if mine_ress[1]>80 then rui:=rui+8;
         for a:=2 to 5 do
             begin
                if mine_ress[a]>50 then rui:=rui+2
                   else if mine_ress[a]>29 then rui:=rui+1;
             end;
       end;
    5: rui:=de(4,1)+5;
    9: rui:=de(2,1)+5;
    10,11: rui:=6;
    else rui:=de(10,1)+5;
  end;
  if sys7.starport>2 then rui:=rui+1;
  if rui<6 then rui:=6;
  sys7.gnp:=evm[trunc(sys7.pop/10)+1]*sys4.attrib[8]*rui;
  sys7.power:=trunc(sys7.gnp*(sys4.attrib[1]+21-sys4.attrib[3])*0.0125
               *mdm[sys4.attrib[8]]);
  if sys7.col_class=10 then sys7.power:=trunc(sys7.power*1.5);
  mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
  case sys7.col_class of
     7,8: sys7.gov:=sys4.gov_type;
     else sys7.gov:=27;
  end;
  sys7.pop_comp:=100;
  if alliance[sys7.race] then
          sys7.pop_comp:=sys7.pop_comp-de((4-sys7.starport)*3,1);
  sys7.misc_char[1]:=0;
  sys7.misc_char[2]:=0;
  sys7.eco_import:=0;
  sys7.eco_export:=0;
  if (sys7.starport<4) and (de(20,1)<=sys4.attrib[1]) and (sys7.pop>30) then set_bit(sys7.misc_char[1],1); {Military Base}
  if (sys7.col_class=7) and (sys4.attrib[1]>9) then set_bit(sys7.misc_char[1],1);
  if (sys7.starport<3) and (de(6,2)>7) and (sys7.pop>30) then set_bit(sys7.misc_char[1],2); {Naval Base}
  if sys7.col_class=10 then set_bit(sys7.misc_char[1],2); {Naval Base}
  if (sys7.pop<40) and (sys7.crime>5) and (sys7.law>5) and (de(6,2)=12) then set_bit(sys7.misc_char[1],3); {Prison Camp}
  if (sys7.pop<40) and (de(100,1)=1) then set_bit(sys7.misc_char[1],4); {Exile Camp}
  if (sys7.pop>50) and (sys4.attrib[8]>6) and (de(10,1)=1) then set_bit(sys7.misc_char[1],5); {University}
  if is_set(sys7.misc_char[1],2) and (de(20,1)=1) then
      begin
         set_bit(sys7.misc_char[1],6); {Military Academy}
         unset_bit(sys7.misc_char[1],2);
      end;
  if (sys7.pop>59) and (de(3,1)=1) and (sys4.attrib[8]>6) then set_bit(sys7.misc_char[1],7); {Arcology}
  if (sys4.attrib[8]>6) and (sys7.pop>54) and (de(10,1)=1) then set_bit(sys7.misc_char[1],8); {Orbital tower}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[1],8))
     then set_bit(sys7.misc_char[2],1); {Ringworld  ** v1.6 addition **}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[2],1))
     then set_bit(sys7.misc_char[2],2); {Planet shield  ** v1.6 addition **}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[2],2))
     then set_bit(sys7.misc_char[2],3); {Space habitats  ** v1.6 addition **}
end;

{*** gov & religion determination based on TL ***}
procedure social_raise(var sys4:alien_record; var aux_str:string; subjugated:boolean);
var rel_change:boolean;
begin
  aux_str:='';
  rel_change:=false;
  if not subjugated then
     case sys4.attrib[8] of
          1,2:sys4.gov_type:=de(7,1);
          3..6: sys4.gov_type:=de(10,1)+4;
          7..10: sys4.gov_type:=de(8,1)+6;
     end;
  if (sys4.attrib[8]=3) and (de(10,1)>=sys4.devotion) then
     begin
       sys4.religion:=de(8,1)+2;
       case sys4.religion of
          8,9: sys4.devotion:=0;
          else begin
                   if sys4.gov_type=7 then sys4.devotion:=de(5,1)+5
                   else sys4.devotion:=de(10,1);
               end;
       end;
       if (sys4.religion=4) and (de(3,1)=1) then sys4.religion:=11;
       rel_change:=true;
     end;
  if sys4.gov_type=9 then
     begin
       case de(20,1) of
         1,2:sys4.gov_type:=15;
         3,4:sys4.gov_type:=16;
         5,6:sys4.gov_type:=17;
         7,8:sys4.gov_type:=18;
         9,10:sys4.gov_type:=19;
         11:sys4.gov_type:=20;
         12:sys4.gov_type:=25;
         13: if sys4.attrib[8]>5 then sys4.gov_type:=24;
       end;
      end;
  if (sys4.gov_type=14) and (sys4.attrib[1]<6) and (sys4.attrib[3]>14)
     then sys4.gov_type:=22;
  if (sys4.gov_type=8) and (sys4.attrib[6]<6) then sys4.gov_type:=23;
  if (sys4.gov_type=13) and (de(100,1)<5) then sys4.gov_type:=21;
  if (sys4.gov_type=7) and (sys4.religion>6) and (sys4.religion<10)
      then sys4.religion:=de(5,1)+1;
  if rel_change then aux_str:=', Religion: '+religion_genre[sys4.religion];

end;

procedure colonize_moon(sys4: alien_record;habit_index:smallint;
  sys3: moon_record;var cyfile:colony_record_file;
  pass:byte;homeworld:boolean;satellite_id:longint;
  allegiance_world:word;system_code:byte);
var factor:byte;
    sys7: colony_record;
begin
  factor:=trunc(sys4.attrib[15]/5);
  sys7.body_type:=2; {2 moon}
  sys7.race:=allegiance_world;
  sys7.allegiance:=sys7.race;
  sys7.world_id:=satellite_id;
  sys7.age:=pass;
  sys7.pop:=de(17,1)+8+(habit_index-5)*2+sys7.age;
  if (is_set(sys4.table_abil[5],8) and (sys7.pop>4))
           then sys7.pop:=sys7.pop-4;  {Infertile}
  if is_set(sys4.table_abil[11],1) then sys7.pop:=sys7.pop+2; {High fecundity}
  if (sys3.world_type>14) and (sys3.world_type<20) and (sys7.pop>10)
            then sys7.pop:=sys7.pop-10;
  if sys7.pop>4 then sys7.pop:=sys7.pop-3;
  sys7.col_class:=3;
  if sys7.pop>39 then sys7.col_class:=2 else
     begin
          if (sys3.world_type>8) and (sys3.world_type<13) then sys7.col_class:=1;
          if (sys3.mine_ress[1]>65) or (sys3.mine_ress[2]>29) or
             (sys3.mine_ress[3]>29) or (sys3.mine_ress[4]>29) or
             (sys3.mine_ress[5]>29) then
                  begin
                    if (sys7.pop<31) then sys7.col_class:=4
                       else sys7.col_class:=3;
                  end;
     end;
  if (sys3.world_type=3) or (sys3.world_type=20) then sys7.col_class:=5;
  if (sys7.pop>9) and (sys7.pop<36) and (de(150,1)=1) and (sys7.age>3) then
           sys7.col_class:=6;
  if ((sys7.pop<6) and (sys3.world_type<>3))
     and ((sys3.world_type<15) or (sys3.world_type>20))then
           begin
              if sys7.age<3 then sys7.col_class:=9
                 else
                    begin
                      if system_code=1 then sys7.col_class:=10
                         else sys7.col_class:=11;
                    end;
           end;
  colonize_proc(sys4,pass,sys7,sys3.mine_ress);
  seek(cyfile,filesize(cyfile));   {Write at the end of file}
  write(cyfile,sys7);
end;



{** CivGen, capital growth with TL rising, new gov **}
procedure colony_growth(sys4:alien_record;pass:byte;var sys7: colony_record;
            mine_ress:table_num;sys2:planet_record;var mfile: moon_record_file;
            var cyfile: colony_record_file;subj_world:boolean);
var   aux:shortint;
      rui,a,b,oldpop,old_starport:byte;
      moon_habit_index:smallint;
      satellite_id:longint;
      sys3:moon_record;
begin
  oldpop:=sys7.pop;
  sys7.pop:=de(15,1)+sys4.attrib[8]*3+30;
  if sys7.pop<oldpop then sys7.pop:=oldpop;
  aux:=de(7,1)+1;
  if sys4.attrib[8]>5 then sys7.col_class:=7;
  if sys4.attrib[8]>8 then aux:=aux+1
     else if sys4.attrib[8]=6 then aux:=aux-2
         else if sys4.attrib[8]<6 then aux:=aux-15;
  if pass>10 then aux:=aux+1
     else if pass<2 then aux:=aux-1;
  if sys7.pop<40 then aux:=aux-1
     else if sys7.pop>49 then aux:=aux+1;
  old_starport:=sys7.starport;
  case aux of
    1,2    : sys7.starport:=4;
    3..6   : sys7.starport:=3;
    7,8    : sys7.starport:=2;
    9..11  : sys7.starport:=1;
    else begin
           if sys4.attrib[8]>5 then sys7.starport:=4
              else sys7.starport:=5;
         end;
  end;
  if old_starport<sys7.starport then sys7.starport:=old_starport;
  case sys4.gov_type of
    1 : sys7.law:=1;
    21: sys7.law:=2;
    3,4,13: sys7.law:=de(5,1)+1;
    12: sys7.law:=de(5,1)+5;
    else sys7.law:=de(9,1)+1;
  end;
  aux:=de(8,1)+trunc((sys4.attrib[5]+sys4.attrib[6]-20)/4);
  if pass>10 then aux:=aux+1
     else aux:=aux-1;
  if aux<1 then aux:=1 else if aux>10 then aux:=10;
  sys7.stability:=aux;
  aux:=de(8,1)+2-trunc(sys7.law/3)-trunc(sys7.stability/3);
  if aux<1 then aux:=1;
  sys7.crime:=aux;
  case sys7.col_class of
    1: rui:=de(7,1)+5;
    4: begin
         rui:=de(5,1);
         if mine_ress[1]>40 then rui:=rui+3
            else if mine_ress[1]>60 then rui:=rui+5
                else if mine_ress[1]>80 then rui:=rui+8;
         for a:=2 to 5 do
             begin
                if mine_ress[a]>50 then rui:=rui+2
                   else if mine_ress[a]>29 then rui:=rui+1;
             end;
       end;
    5: rui:=de(4,1)+5;
    9: rui:=de(2,1)+5;
    10,11: rui:=6;
    else rui:=de(10,1)+5;
  end;
  if sys7.starport>2 then rui:=rui+1;
  if rui<6 then rui:=6;
  mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]-sys7.power;
  sys7.gnp:=evm[trunc(sys7.pop/10)+1]*sys4.attrib[8]*rui;
  sys7.power:=trunc(sys7.gnp*(sys4.attrib[1]+21-sys4.attrib[3])*0.0125
               *mdm[sys4.attrib[8]]);
  if sys7.col_class=10 then sys7.power:=trunc(sys7.power*1.5);
  mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
  if subj_world then sys7.gov:=26 else
    case sys7.col_class of
       7,8: sys7.gov:=sys4.gov_type;
       else sys7.gov:=27;
    end;
  sys7.pop_comp:=100;
  if alliance[sys7.race] then
          sys7.pop_comp:=sys7.pop_comp-de((4-sys7.starport)*3,1);
  sys7.misc_char[1]:=0;
  sys7.misc_char[2]:=0;
  if (sys7.starport<4) and (de(20,1)<=sys4.attrib[1]) and (sys7.pop>30) then set_bit(sys7.misc_char[1],1); {Military Base}
  if (sys7.col_class=7) and (sys4.attrib[1]>9) then set_bit(sys7.misc_char[1],1);
  if (sys7.starport<3) and (de(6,2)>7) and (sys7.pop>30) then set_bit(sys7.misc_char[1],2); {Naval Base}
  if sys7.col_class=10 then set_bit(sys7.misc_char[1],2); {Naval Base}
  if (sys7.pop<40) and (sys7.crime>5) and (sys7.law>5) and (de(6,2)=12) then set_bit(sys7.misc_char[1],3); {Prison Camp}
  if (sys7.pop<40) and (de(120,1)=1) then set_bit(sys7.misc_char[1],4); {Exile Camp}
  if (sys7.pop>50) and (sys4.attrib[8]>6) and (de(12,1)=1) then set_bit(sys7.misc_char[1],5); {University}
  if is_set(sys7.misc_char[1],2) and (de(25,1)=1) then
      begin
         set_bit(sys7.misc_char[1],6); {Military Academy}
         unset_bit(sys7.misc_char[1],2);
      end;
  if (sys7.pop>59) and (de(4,1)=1) and (sys4.attrib[8]>6) then set_bit(sys7.misc_char[1],7); {Arcology}
  if (sys4.attrib[8]>6) and (sys7.pop>54) and (de(12,1)=1) then set_bit(sys7.misc_char[1],8); {Orbital tower}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[1],8))
     then set_bit(sys7.misc_char[2],1); {Ringworld  ** v1.6 addition **}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[2],1))
     then set_bit(sys7.misc_char[2],2); {Planet shield  ** v1.6 addition **}
  if (sys4.attrib[8]>8) and (sys7.pop>54) and (de(10,1)<3) and (not is_set(sys7.misc_char[2],2))
     then set_bit(sys7.misc_char[2],3); {Space habitats  ** v1.6 addition **}

  if (sys7.allegiance=sys7.race) and (sys4.attrib[8]>5) and (pass>0) then
    begin
      for a:=1 to sys2.satellites do   {** Colonizing moons **}
        begin
          satellite_id:=sys2.moon_id+a-1;
          seek(mfile,satellite_id);
          read(mfile,sys3);
          if not (is_set(sys3.misc_charac,4)) then
             begin
                moon_habit_index:=habitability[sys3.world_type];
                if sys3.mine_ress[1]<25 then moon_habit_index:=moon_habit_index-1
                   else if sys3.mine_ress[1]>50 then moon_habit_index:=moon_habit_index+1
                        else if sys3.mine_ress[1]>75 then moon_habit_index:=moon_habit_index+2;
                for b:=2 to 5 do
                   if sys3.mine_ress[b]>50 then moon_habit_index:=moon_habit_index+1;
                {Higher chances of inhabited moons around homeworlds}
                moon_habit_index:=moon_habit_index+sys4.attrib[8]-4;
                if moon_habit_index<1 then moon_habit_index:=1
                 else if moon_habit_index>10 then moon_habit_index:=10;
                if de(12,1)<=moon_habit_index then
                   begin
                     colonize_moon(sys4,8,sys3,cyfile,
                     pass,false,satellite_id,sys7.allegiance,0);
                     seek(mfile,satellite_id);
                     set_bit(sys3.misc_charac,4);
                     write(mfile,sys3);
                   end;
             end;
        end;
    end;
end;




{homeworld:   0=native homeworld
              1=capital(this race comes from another sector)
              2=colony}
procedure colonize_planet(sys4: alien_record;habit_index:smallint;
  var sys7: colony_record; sys1,init_star:star_record;var sys2: planet_record;
  pass:byte;planet,star:longint;homeworld:byte; var mfile: moon_record_file;
  var cyfile:colony_record_file;var tfile,evfile:textfile);
var factor,a,b:byte;
    moon_habit_index:shortint;
    satellite_id:longint;
    sys3: moon_record;
begin
  factor:=trunc(sys4.attrib[15]/5);
  sys7.body_type:=1; {1 planet}
  if homeworld=0 then
     begin
        sys7.race:=sys2.allegiance;
        sys7.allegiance:=sys7.race;
        sys7.world_id:=abs(sys4.pln_id);
        sys7.age:=255;
        sys7.pop:=de(15,1)+sys4.attrib[8]*3+30;
        if (is_set(sys4.table_abil[5],8) and (sys7.pop>4))
           then sys7.pop:=sys7.pop-4;  {Infertile}
        if is_set(sys4.table_abil[11],1) then sys7.pop:=sys7.pop+3; {High fecundity}
        if sys7.pop>69 then sys7.pop:=69;
        if sys4.attrib[8]<6 then sys7.col_class:=8
           else sys7.col_class:=7;;
        if (de(200,1)+9)<sys4.attrib[13] then set_bit(sys2.unusual[4],8);{Wonder}
        if de(750,1)<sys4.devotion then set_bit(sys2.unusual[5],1);{Holy site}
        homeplanet[sys7.race]:=filesize(cyfile);
     end
     else
     begin
        case homeworld of
          1: begin
               sys7.race:=sys2.allegiance;
               sys7.world_id:=abs(sys4.pln_id);
               homeplanet[sys7.race]:=filesize(cyfile);
             end;
          2: begin
               sys7.race:=sys1.allegiance;
               sys7.world_id:=sys1.pln_id[star]-1+planet;
             end;
        end;
        sys7.allegiance:=sys7.race;
        sys7.age:=pass;
        sys7.pop:=de(17,1)+8+(habit_index-5)*2+sys7.age;
        if (is_set(sys4.table_abil[5],8) and (sys7.pop>4))
           then sys7.pop:=sys7.pop-4;  {Infertile}
        if is_set(sys4.table_abil[11],1) then sys7.pop:=sys7.pop+3; {High fecundity}
        if (sys2.world_type>14) and (sys2.world_type<20) and (sys7.pop>10)
            then sys7.pop:=sys7.pop-10;
        sys7.col_class:=3;
        if sys7.pop>39 then sys7.col_class:=2 else
           begin
             if (sys2.world_type>8) and (sys2.world_type<13) then sys7.col_class:=1;
             if (sys2.mine_ress[1]>65) or (sys2.mine_ress[2]>29) or
                (sys2.mine_ress[3]>29) or (sys2.mine_ress[4]>29) or
                (sys2.mine_ress[5]>29) then
                  begin
                    if (sys7.pop<31) then sys7.col_class:=4
                       else sys7.col_class:=3;
                  end;
           end;
        if (sys2.world_type=3) or (sys2.world_type=20) then sys7.col_class:=5;
        if (sys7.pop>9) and (sys7.pop<36) and (de(150,1)=1) and (sys7.age>3) then
           sys7.col_class:=6;
        if ((sys7.pop<6) and (sys2.world_type<>3))
           and ((sys2.world_type<15) or (sys2.world_type>20))then
           begin
              if sys7.age<3 then sys7.col_class:=9
                 else sys7.col_class:=11;
           end;
        if homeworld=1 then
            begin
              sys7.col_class:=7;
              if sys7.pop<40 then sys7.pop:=sys7.pop+15;
            end;
        if (Form1.CheckBox1.Checked) and (homeworld=2) then
           writeln(tfile,'Info  : Race ',sys7.race:4,' founds a colony on planet ',sys7.world_id,' (System ',sys2.sun_id,')');
        if (Form1.CheckBox8.Checked) and (homeworld=2) then
           writeln(evfile,pass,';Info;',sys7.race:4,';;',sys7.world_id,';',sys2.sun_id,';Colony founded');
        if (de(1000,1)+10)<sys4.attrib[13] then set_bit(sys2.unusual[4],8);{Wonder of the galaxy}
        if de(1500,1)<sys4.devotion then set_bit(sys2.unusual[5],1);{Holy site}
        if sys7.pop>49 then set_bit(sys2.unusual[5],4); {High population}
     end;
     colonize_proc(sys4,pass,sys7,sys2.mine_ress);
     {To avoid a revolution on a non sector alien capital }
     if (homeworld=1) and (sys7.stability<3) then sys7.stability:=3;
     seek(cyfile,filesize(cyfile));
     write(cyfile,sys7);
     if (sys4.attrib[8]>5) and (homeworld=2) then
      for a:=1 to sys2.satellites do
        begin
          satellite_id:=sys2.moon_id+a-1;
          seek(mfile,satellite_id);
          read(mfile,sys3);
          moon_habit_index:=habitability[sys3.world_type];
          if habit_index<1 then moon_habit_index:=moon_habit_index-1;
          if sys3.mine_ress[1]<25 then moon_habit_index:=moon_habit_index-1
             else if sys3.mine_ress[1]>50 then moon_habit_index:=moon_habit_index+1
                else if sys3.mine_ress[1]>75 then moon_habit_index:=moon_habit_index+2;
          for b:=2 to 5 do
             if sys3.mine_ress[b]>50 then moon_habit_index:=moon_habit_index+1;
          {Higher chances of inhabited moons around homeworlds}
          if homeworld<2 then moon_habit_index:=moon_habit_index+sys4.attrib[8]-4;
          if moon_habit_index<1 then moon_habit_index:=1
                 else if moon_habit_index>10 then moon_habit_index:=10;
          if habit_index<1 then moon_habit_index:=moon_habit_index-1;
          if de(10,1)<=moon_habit_index then
             begin
                   colonize_moon(sys4,habit_index,sys3,cyfile,
                   pass,false,satellite_id,sys7.allegiance,sys1.code);
                   seek(mfile,satellite_id);
                   set_bit(sys3.misc_charac,4);
                   write(mfile,sys3);
              end;
        end;
end;

function habit_planet_calcul(sys2:planet_record):byte;
var habit_index:shortint;
    a:byte;
    bad_cond,vbad_cond,extreme_cond:boolean;
begin
      habit_index:=habitability[sys2.world_type];
      bad_cond:=false;
      vbad_cond:=false;
      extreme_cond:=false;
      if is_set(sys2.unusual[1],1) then bad_cond:=true;
      if is_set(sys2.unusual[1],2) then vbad_cond:=true;
      if is_set(sys2.unusual[1],3) then vbad_cond:=true;
      if is_set(sys2.unusual[1],4) then extreme_cond:=true;
      if is_set(sys2.unusual[1],5) then bad_cond:=true;
      if is_set(sys2.unusual[1],6) then vbad_cond:=true;
      if is_set(sys2.unusual[1],7) then vbad_cond:=true;
      if is_set(sys2.unusual[2],2) then bad_cond:=true;
      if is_set(sys2.unusual[2],3) then vbad_cond:=true;
      if is_set(sys2.unusual[2],4) then extreme_cond:=true;
      if is_set(sys2.unusual[3],7) then vbad_cond:=true;
      if is_set(sys2.unusual[3],8) then extreme_cond:=true;
      if extreme_cond then habit_index:=habit_index-5
         else if vbad_cond then habit_index:=habit_index-3
              else if bad_cond then habit_index:=habit_index-1;
      if sys2.mine_ress[1]<25 then habit_index:=habit_index-1
         else if sys2.mine_ress[1]>50 then habit_index:=habit_index+1
              else if sys2.mine_ress[1]>75 then habit_index:=habit_index+2;
      for a:=2 to 5 do
          if sys2.mine_ress[a]>50 then habit_index:=habit_index+1;
      if habit_index<0 then habit_index:=0
         else if habit_index>10 then habit_index:=10;
      if ((sys2.world_type=3) or (sys2.world_type=20)) and (habit_index=0) then
         habit_index:=1;
      habit_planet_calcul:=habit_index;
end;

{Old: true if the system was already owned by the alien race
 Newcol: true if a planet is colonized}
procedure colonize_system(var sys1:star_record;init_star:star_record; var pfile:planet_record_file;
    sys4: alien_record; var cyfile: colony_record_file; var mfile: moon_record_file; pass:byte;
    war,old:boolean; defender:word;alien_id:word; var tfile:textfile;power1,power2:single;
    var ctfile:contact_record_file;var newcol:boolean;var evfile:textfile);
var star,planet:byte;
    temp:word;
    habit_index:shortint;
    sys2:planet_record;
    sys7:colony_record;
    aux:longint;
    auxstr:string;
begin
   newcol:=false;
   for star:=1 to sys1.star_nbr do
     begin
       for planet:=1 to sys1.planet_nbr[star] do
         begin
           seek(pfile,sys1.pln_id[star]-1+planet);
           read(pfile,sys2);
           if (sys2.allegiance=65535) and (defender=65535) then
             begin
              if habit_table[sys1.pln_id[star]-1+planet]=255
                 then habit_index:=habit_planet_calcul(sys2)
                 else habit_index:=habit_table[sys1.pln_id[star]-1+planet];
              if Form1.CheckBox2.Checked then temp:=de(10,1)
                 else if old then temp:=de(100,1)
                    else temp:=de(11,1);
              if temp<=habit_index then
                 begin
                   colonize_planet(sys4,habit_index,sys7,sys1,init_star,
                     sys2,pass,planet,star,2,mfile,cyfile,tfile,evfile);
                   sys2.allegiance:=alien_id;
                   set_bit(sys2.misc_charac,4);
                   seek(pfile,sys1.pln_id[star]-1+planet);
                   write(pfile,sys2);
                   newcol:=true;
                 end;
             end
             else
                begin
                   if war and (is_set(sys2.misc_charac,4)) then
                         begin
                           sys2.allegiance:=alien_id;
                           if Form1.CheckBox1.Checked then writeln(tfile,'Battle: Planet ',sys1.pln_id[star]-1+planet,
                              ' conquered by race ',alien_id,' from race ',defender,' (System ',sys2.sun_id,')');
                           if Form1.CheckBox8.Checked then writeln(evfile,pass,';Battle;',alien_id,';',defender,
                              ';',sys1.pln_id[star]-1+planet,';',sys2.sun_id,';Planet conquered by race ',alien_id);
                           if (power1>5000) and (power2>5000) and (power1+power2>15000) and (de(5,1)=1) then
                              begin
                                if pass<10 then set_bit(sys2.unusual[4],7);
                              end;
                           if (power1>(2*victory_value*power2)) and (power2>1000) and (de(2,1)=1) and
                                (sys4.attrib[1]>12) then
                              begin
                                 if pass<5 then set_bit(sys2.unusual[4],5);
                              end;
                           seek(pfile,sys1.pln_id[star]-1+planet);
                           write(pfile,sys2);
                         end;
                end;
           end;
     end;
end;


procedure tl_uplifting(empire_id:word;var sys4:alien_record;sys8:contact_record);{oldtl:byte;
  var sys7:colony_record);}
begin
  rp[empire_id]:=-1;
  if (sys4.attrib[15]=0) and (sys4.attrib[8]>5) then sys4.attrib[15]:=sys8.age;
end;


procedure contact_proc(var sys8:contact_record;var sys1,init_star:star_record;var sys4,a_temp:alien_record;
    var cyfile:colony_record_file;var tfile:textfile;var pfile:planet_record_file;var mfile:moon_record_file;
    var afile:alien_record_file;var sfile:star_record_file;alien1:integer;pass:byte;
    var ctfile:contact_record_file;var evfile:textfile);
var diplo,dice,old_TL:byte;
    sys7a,sys7b:colony_record;
    sys2:planet_record;
    sysdef:star_record;
    power1,power2,diff:single;
    newcol:boolean;
    aux_str:string;
begin
                diplo:=sys4.attrib[3]+a_temp.attrib[3];
                dice:=random(40);
                if dice>diplo then
                   begin
                     if Form1.CheckBox1.Checked then writeln(tfile,' War!');
                     if Form1.CheckBox8.Checked then writeln(evfile,' War!');
                     sys8.relation:=1;
                     rp_diplo[sys8.empire1]:=1;
                     rp_diplo[sys8.empire2]:=1;
                     diplo_table[sys8.empire1,sys8.empire2]:=1;
                     diplo_table[sys8.empire2,sys8.empire1]:=1;
                     sys1.code:=1;
                     seek(cyfile,homeplanet[alien1]);
                     read(cyfile,sys7a);
                     seek(cyfile,homeplanet[sys1.allegiance]);
                     read(cyfile,sys7b);
                     seek(pfile,sys7b.world_id);
                     read(pfile,sys2);
                     seek(sfile,sys2.sun_id);
                     read(sfile,sysdef);
                     diff:=sqrt(sqr(init_star.PosX-Sys1.PosX)+sqr(init_star.PosY-Sys1.PosY)
                       +sqr(init_star.PosZ-Sys1.PosZ));
                     diff:=(dist_table[sys4.attrib[8]-5]-diff)/dist_table[sys4.attrib[8]-5];
                     if diff<0.4 then diff:=0.4
                        else if diff>1 then diff:=1;
                     if sys4.pln_id<0 then
                           begin
                            power1:=mi_power[alien1]*diff*(1.5-random(10)/10);
                            if power1<((sys4.attrib[8]-5)*mibonus*0.3) then power1:=(sys4.attrib[8]-5)*mibonus*0.3;
                           end
                        else power1:=mi_power[alien1]*diff*(1.5-random(10)/10);
                     diff:=sqrt(sqr(sysdef.PosX-Sys1.PosX)+sqr(sysdef.PosY-Sys1.PosY)
                       +sqr(sysdef.PosZ-Sys1.PosZ));
                     if a_temp.attrib[8]>5 then
                        diff:=(dist_table[a_temp.attrib[8]-5]-diff)/dist_table[a_temp.attrib[8]-5]
                       else diff:=1;
                     if diff<0.4 then diff:=0.4
                        else if diff>1 then diff:=1;
                     if a_temp.pln_id<0 then
                         begin
                          power2:=mi_power[sys1.allegiance]*diff*(1.5-random(10)/10);
                          if power2<((a_temp.attrib[8]-5)*mibonus) then power2:=(a_temp.attrib[8]-5)*mibonus;
                         end
                        else power2:=mi_power[sys1.allegiance]*diff*(1.5-random(10)/10);
                     {writeln(tfile,'  Power1:',power1:0:0,' Power2:',power2:0:0);}
                     if power1>(victory_value*power2) then
                         begin
                           colonize_system(sys1,init_star,pfile,sys4,cyfile,mfile,pass,
                             true,false,sys1.allegiance,alien1,tfile,power1,power2,ctfile,newcol,evfile);
                           sys1.allegiance:=alien1;
                         end;
                   end
                   else
                   if dice>(0.75*diplo) then
                          begin
                             if Form1.CheckBox1.Checked then writeln(tfile,' No intercourse!');
                             if Form1.CheckBox8.Checked then writeln(evfile,' No intercourse!');
                             diplo_table[sys8.empire1,sys8.empire2]:=2;
                             diplo_table[sys8.empire2,sys8.empire1]:=2;
                             sys8.relation:=2;
                          end
                        else
                        if dice>(0.5*diplo) then
                               begin
                                  if Form1.CheckBox1.Checked then writeln(tfile,' Trade!');
                                  if Form1.CheckBox8.Checked then writeln(evfile,' Trade!');
                                  diplo_table[sys8.empire1,sys8.empire2]:=3;
                                  diplo_table[sys8.empire2,sys8.empire1]:=3;
                                  if (a_temp.attrib[8]>sys4.attrib[8]) and (rp_diplo[sys8.empire1]=0)
                                    then rp_diplo[sys8.empire1]:=2;
                                  if (sys4.attrib[8]>a_temp.attrib[8]) and (rp_diplo[sys8.empire2]=0)
                                    then rp_diplo[sys8.empire2]:=2;
                                  sys8.relation:=3;
                               end
                        else
                        begin
                             if Form1.CheckBox1.Checked then writeln(tfile,' Alliance!');
                             if Form1.CheckBox8.Checked then writeln(evfile,' Alliance!');
                             alliance[sys8.empire1]:=true;
                             alliance[sys8.empire2]:=true;
                             diplo_table[sys8.empire1,sys8.empire2]:=4;
                             diplo_table[sys8.empire2,sys8.empire1]:=4;
                             sys8.relation:=4;
                             if (a_temp.attrib[8]>5) and (a_temp.attrib[15]<(pass+1))
                                then begin  {Spatial age raised }
                                        a_temp.attrib[15]:=pass+1;
                                        seek(afile,sys8.empire2);
                                        write(afile,a_temp);
                                        if Form1.CheckBox1.Checked then
                                         writeln(tfile,'        Race ',sys8.empire2,
                                          ' goes to stellar exploration after the contact.');
                                        if Form1.CheckBox8.Checked then writeln(evfile,pass,
                                          ';Info;',sys8.empire2,';;;;The race goes to stellar',
                                          ' exploration after the contact.');
                                     end;
                             if sys4.attrib[8]>(a_temp.attrib[8]+1) then
                                    begin
                                      if Form1.CheckBox1.Checked then
                                         write(tfile,'        TL uplifting from ',a_temp.attrib[8],' to ');
                                      if Form1.CheckBox8.Checked then
                                         write(evfile,pass,';Civ;',sys8.empire2,';;;;','TL uplifting from ',a_temp.attrib[8],' to ');
                                      tladvance[sys8.empire2]:=sys4.attrib[8]-a_temp.attrib[8]-1;
                                      old_TL:=a_temp.attrib[8];
                                      a_temp.attrib[8]:=sys4.attrib[8]-1;
                                      tl_uplifting(sys8.empire2,a_temp,sys8); {New proc}
                                      if old_TL<6 then
                                         begin
                                             social_raise (a_temp,aux_str,false);
                                             aux_str:=' (New gov : '+gov[a_temp.gov_type]+aux_str+')';
                                          end;
                                      seek(afile,sys8.empire2);
                                      write(afile,a_temp);
                                      if Form1.CheckBox1.Checked then writeln(tfile,a_temp.attrib[8],aux_str);
                                      if Form1.CheckBox8.Checked then writeln(evfile,a_temp.attrib[8],aux_str);
                                      if a_temp.attrib[8]>5 then
                                        begin
                                           colonize_system(sys1,sys1,pfile,a_temp,cyfile,
                                              mfile,pass,false,false,65535,sys8.empire2,tfile,power1,
                                              power2,ctfile,newcol,evfile);
                                           {Info about newly high TL race colonizing space}
                                        end;
                                    end;
                             if a_temp.attrib[8]>(sys4.attrib[8]+1) then
                                    begin
                                      if Form1.CheckBox1.Checked then
                                         write(tfile,'        TL uplifting from ',sys4.attrib[8],' to ');
                                      if Form1.CheckBox8.Checked then
                                         write(evfile,pass,';Civ;',sys8.empire1,';;;;','TL uplifting from ',sys4.attrib[8],' to ');
                                      tladvance[sys8.empire1]:=a_temp.attrib[8]-sys4.attrib[8]-1;
                                      old_TL:=sys4.attrib[8];
                                      sys4.attrib[8]:=a_temp.attrib[8]-1;
                                      tl_uplifting(sys8.empire1,sys4,sys8); {New proc}
                                      if old_TL<6 then
                                         begin
                                            social_raise (sys4,aux_str,false);
                                            aux_str:=' (New gov : '+gov[sys4.gov_type]+aux_str+')';
                                         end;   
                                      seek(afile,sys8.empire1);
                                      write(afile,sys4);
                                      if Form1.CheckBox1.Checked then writeln(tfile,sys4.attrib[8],aux_str);
                                      if Form1.CheckBox8.Checked then writeln(evfile,sys4.attrib[8],aux_str);
                                    end;
                        end;
end;

procedure init_colonize_system(alien1:integer;var sys1:star_record;
   var sys4:alien_record;var ctfile:contact_record_file; var afile:alien_record_file;
   var pfile:planet_record_file;var cyfile:colony_record_file; var sfile:star_record_file;
   var mfile:moon_record_file;pass: byte;var tfile:textfile;system_id:longint; var evfile:textfile);
var aux,aux1,aux2,aux3:string;
    a_temp:alien_record;
    new_contact,newcol:boolean;
    sys2:planet_record;
    sys7a,sys7b:colony_record;
    sys8:contact_record;
    init_star,sysdef:star_record;
    relation_state:byte;
    factor:byte;
    power1,power2,diff:single;
begin
   str(sys1.posX,aux1);
   str(sys1.posY,aux2);
   str(sys1.posZ,aux3);
   seek(pfile,abs(sys4.pln_id));
   read(pfile,sys2);
   seek(sfile,sys2.sun_id);
   read(sfile,init_star);
   if sys1.allegiance=alien1 then
            begin
             colonize_system(sys1,init_star,pfile,sys4,cyfile,mfile,pass,false,true,65535,alien1,tfile,
               power1,power2,ctfile,newcol,evfile);
            end;
   if (sys1.allegiance<>65535) and (sys1.allegiance<>alien1) then
      begin
         seek(afile,sys1.allegiance);
         read(afile,a_temp);
         new_contact:=false;
         relation_state:=diplo_table[alien1,sys1.allegiance];
         if diplo_table[alien1,sys1.allegiance]=0 then
               new_contact:=true;
         if new_contact then
            begin
              {You can't contact not yet installed non sector aliens}
              if (a_temp.pln_id>-1) or (a_temp.attrib[15]>pass) then
                begin
                  if Form1.CheckBox1.Checked then
                    begin
                      str(alien1,aux1);
                      str(sys1.allegiance,aux2);
                      aux:='Diplo : Contact between race '+aux1+' and race '+aux2+' at';
                      write(tfile,aux,' system:',system_id);
                    end;
                  if Form1.CheckBox8.Checked then
                    begin
                      str(alien1,aux1);
                      str(sys1.allegiance,aux2);
                      write(evfile,pass,';Diplo;',aux1,';',aux2,';;',system_id,
                        ';First Contact:');
                    end;
                  factor:=trunc(sys4.attrib[15]/5);
                  sys8.age:=pass;
                  sys8.empire1:=alien1;
                  sys8.empire2:=sys1.allegiance;
                  contact_proc(sys8,sys1,init_star,sys4,a_temp,cyfile,tfile,pfile,mfile,
                    afile,sfile,alien1,pass,ctfile,evfile);
                  seek(ctfile,filesize(ctfile));
                  file_table[sys8.empire1,sys8.empire2]:=filesize(ctfile);
                  file_table[sys8.empire2,sys8.empire1]:=filesize(ctfile);
                  write(ctfile,sys8);
                end;
            end;
         if (not new_contact) then
            begin  {Not a new contact}
                if relation_state=1 then
                     begin
                        seek(cyfile,homeplanet[alien1]);
                        read(cyfile,sys7a);
                        seek(cyfile,homeplanet[sys1.allegiance]);
                        read(cyfile,sys7b);
                        seek(pfile,sys7b.world_id);
                        read(pfile,sys2);
                        seek(sfile,sys2.sun_id);
                        read(sfile,sysdef);
                        diff:=sqrt(sqr(init_star.PosX-Sys1.PosX)+sqr(init_star.PosY-Sys1.PosY)
                          +sqr(init_star.PosZ-Sys1.PosZ));
                        diff:=(dist_table[sys4.attrib[8]-5]-diff)/dist_table[sys4.attrib[8]-5];
                        if diff<0.4 then diff:=0.4
                          else if diff>1 then diff:=1;
                        if sys4.pln_id<0 then
                             begin
                               power1:=mi_power[alien1]*diff*(1.5-random(10)/10);
                               if power1<((sys4.attrib[8]-5)*mibonus*0.3) then power1:=(sys4.attrib[8]-5)*mibonus*0.3;
                             end
                           else power1:=mi_power[alien1]*diff*(1.5-random(10)/10);
                        diff:=sqrt(sqr(sysdef.PosX-Sys1.PosX)+sqr(sysdef.PosY-Sys1.PosY)
                          +sqr(sysdef.PosZ-Sys1.PosZ));
                        if a_temp.attrib[8]>5 then
                           diff:=(dist_table[a_temp.attrib[8]-5]-diff)/dist_table[a_temp.attrib[8]-5]
                          else diff:=1;
                        if diff<0.4 then diff:=0.4
                          else if diff>1 then diff:=1;
                        if a_temp.pln_id<0 then
                             begin
                               power2:=mi_power[sys1.allegiance]*diff*(1.5-random(10)/10);
                               if power2<((a_temp.attrib[8]-5)*mibonus) then power2:=(a_temp.attrib[8]-5)*mibonus;
                             end
                           else power2:=mi_power[sys1.allegiance]*diff*(1.5-random(10)/10);
                        sys1.code:=1; {If races in war the system code is Red}
                        if power1>(victory_value*power2) then
                         begin
                           colonize_system(sys1,init_star,pfile,sys4,cyfile,mfile,pass,
                             true,false,sys1.allegiance,alien1,tfile,power1,power2,ctfile,newcol,evfile);
                           sys1.allegiance:=alien1;
                           diff:=sqrt(sqr(init_star.PosX-Sys1.PosX)+sqr(init_star.PosY-Sys1.PosY)
                              +sqr(init_star.PosZ-Sys1.PosZ));
                           if diff>max_dist[alien1] then max_dist[alien1]:=round(diff);
                         end;
                     end;
            end;
      end;
   if sys1.allegiance=65535 then
            begin
             sys1.allegiance:=alien1;
             colonize_system(sys1,init_star,pfile,sys4,cyfile,mfile,pass,false,false,65535,alien1,tfile,
               power1,power2,ctfile,newcol,evfile);
             if not newcol then sys1.allegiance:=65535
               else begin
                        diff:=sqrt(sqr(init_star.PosX-Sys1.PosX)+sqr(init_star.PosY-Sys1.PosY)
                          +sqr(init_star.PosZ-Sys1.PosZ));
                        if diff>max_dist[alien1] then max_dist[alien1]:=round(diff);
                    end;
            end;
end;

procedure Init_system(var afile:alien_record_file;var sfile:star_record_file;
   var pfile:planet_record_file; var cyfile:colony_record_file; var mfile:moon_record_file;
   var tfile,evfile:textfile);
var a: longint;
    power1,power2:single;
    sys1:star_record;
    sys2:planet_record;
    sys4,sys4b:alien_record;
    sys7:colony_record;
begin
  for a:=0 to (filesize(afile)-1) do
     begin
       seek(afile,a);
       read(afile,sys4);
       seek(pfile,abs(sys4.pln_id));
       read(pfile,sys2);
       seek(pfile,abs(sys4.pln_id));
       seek(sfile,sys2.sun_id);
       read(sfile,sys1);
       {**v1.6 Fix the non ingerance bug **}
       if sys1.allegiance=65535 then
          begin
             sys1.allegiance:=a;
             seek(sfile,sys2.sun_id);
             write(sfile,sys1);
          end;
       sys2.allegiance:=a;
       if sys4.app_genre=18 then  {**v1.6 Mechanoid races are considered like extra sector races**}
          begin
             sys4.pln_id:=-abs(sys4.pln_id);
             if sys4.attrib[8]<6 then sys4.attrib[8]:=6;
             seek(afile,a);
             write(afile,sys4);
          end;
       if sys4.pln_id>-1 then
          begin
            if civ_factor then
               begin
                 sys4.attrib[8]:=1;  {**Set TL to 1**}
                 sys4.attrib[15]:=0; {**Set spatial age to 0 **}
                 sys4.gov_type:=de(7,1); {**Gov setting **}
                 sys4.religion:=de(5,1); {**Religion setting**}
                 seek(afile,a);
                 write(afile,sys4);
               end;
            colonize_planet(sys4,0,sys7,sys1,sys1,sys2,1,0,0,0,mfile,cyfile,tfile,evfile);
          end
         else  colonize_planet(sys4,0,sys7,sys1,sys1,sys2,sys4.attrib[15],0,0,1,mfile,cyfile,tfile,evfile);
       set_bit(sys2.misc_charac,4);
       seek(pfile,abs(sys4.pln_id));
       write(pfile,sys2);
       if sys4.attrib[8]>5 then
          begin
             if sys1.allegiance<>65535 then
                begin
                     power1:=Empire_power(sys4,1);
                     seek(afile,sys1.allegiance);
                     read(afile,sys4b);
                     power2:=Empire_power(sys4,1);
                     writeln(tfile,'Info  : 2+ races in system ',sys2.sun_id);
                     if power1>power2 then sys1.allegiance:=a;
                 end;
             seek(sfile,sys2.sun_id);
             write(sfile,sys1);
          end;
     end;
     writeln(tfile);
end;

procedure allied_free(var pfile:planet_record_file;var sys2:planet_record; var sys7:colony_record;
  var tfile,evfile:textfile;pass:byte; var afile:alien_record_file);
var sys4: alien_record;
begin
   if sys7.law>3 then sys7.law:=sys7.law-2;
   if sys7.stability<9 then sys7.stability:=sys7.stability+1;
   if sys7.body_type=1 then
      begin
        if Form1.CheckBox1.Checked then writeln(tfile,'Battle: Planet ',sys7.world_id,
          ' freed by occupation by race ',sys2.allegiance,' from race ',sys7.allegiance);
        if Form1.CheckBox8.Checked then writeln(evfile,pass,';Battle;',sys2.allegiance,';',
          sys7.allegiance,';',sys7.world_id,';;Planet freed by occupation by race ',sys2.allegiance,
          ' from race ',sys7.allegiance);
        sys2.allegiance:=sys7.race;
        seek(pfile,sys7.world_id);
        write(pfile,sys2);
      end;
   sys7.allegiance:=sys7.race;
   seek(afile,sys7.race);
   read(afile,sys4);
   sys7.gov:=sys4.gov_type;
end;

procedure save_empire(var cyfile:colony_record_file;var ctfile:contact_record_file;
    var afile:alien_record_file);
var efile: empire_record_file;
    sys9 : empire_record;
    sys4 : alien_record;
    a    : longint;
begin
    assign (efile,concat(namefile,'.emp'));
    rewrite(efile);
    main_colony_emp(namefile,cyfile,ctfile,afile);
    reset(afile);
    Form1.StatusBar1.SimpleText:='Saving alien files';
    for a:=0 to (filesize(afile)-1) do
      begin
        sys9.attrib[1]:=economic_power[a];
        sys9.attrib[2]:=mi_power[a];
        sys9.attrib[3]:=total_pop[a];
        sys9.attrib[4]:=trade_bonus[a];
        if is_set(sys4.table_abil[8],6) then
           sys9.attrib[4]:=trunc(sys9.attrib[4]*1.05);
        sys9.attrib[5]:=captive_pop[a];
        sys9.attrib[6]:=indep_pop[a];
        sys9.attrib[7]:=subject_pop[a];
        sys9.attrib[8]:=colony_nbr[a];
        sys9.attrib[9]:=captive_world_nbr[a];
        sys9.attrib[10]:=subj_world[a];
        sys9.attrib[11]:=moon_nbr[a];
        sys9.attrib[12]:=subj_moon[a];
        sys9.attrib[13]:=indep_col[a];
        write(efile,sys9);
        seek(afile,a);
        read(afile,sys4);
        if sys4.attrib[8]<5 then  {No bonus to HiTech skills for LowTech species}
           begin
             if is_set(sys4.table_abil[8],7) then unset_bit(sys4.table_abil[8],7);
             if is_set(sys4.table_abil[8],8) then unset_bit(sys4.table_abil[8],8);
             if is_set(sys4.table_abil[9],2) then unset_bit(sys4.table_abil[9],2);
             if is_set(sys4.table_abil[11],2) then unset_bit(sys4.table_abil[11],2);
             if is_set(sys4.table_abil[11],3) then unset_bit(sys4.table_abil[11],3);
           end;
        case (de(60,1)+sys4.attrib[8]*5) of  {Recalculating lifespan based on TL }
             1..10 : sys4.attrib[12]:=de(5,1)+1;
             11..25: sys4.attrib[12]:=de(10,1)+2;
             26..45: sys4.attrib[12]:=de(15,1)+4;
             46..70: sys4.attrib[12]:=de(30,1)+8;
             71..80: sys4.attrib[12]:=de(60,1)+12;
             81..90: sys4.attrib[12]:=de(100,1)+25;
             else sys4.attrib[12]:=de(200,1)+50;
        end;
        if is_set(sys4.table_abil[4],8) then
           begin
              if sys4.attrib[12]<100 then sys4.attrib[12]:=sys4.attrib[12]*2;
              if sys4.attrib[12]<10 then sys4.attrib[12]:=10;
           end;
        seek(afile,a);
        write(afile,sys4);
      end;
    closefile(efile);
    closefile(afile);
end;

{** CivGen Low TL pass **}
procedure main_lowtech (var afile: alien_record_file; var tfile:textfile;
  var pfile: planet_record_file; var cyfile: colony_record_file; pass: byte;
  var evfile:textfile;var mfile: moon_record_file);
var TL: byte;
    d:word;
    limit_cost: smallint;
    sys4: alien_record;
    sys2: planet_record;
    sys7: colony_record;
    aux_str:string;
    subj_worl:boolean;
const tl_cost: array [1..5] of word= (200,50,30,30,40);
begin
   for TL:=5 downto 1 do
       begin
         for d:=0 to (filesize(afile)-1) do
             begin
                seek(afile,d);
                read(afile,sys4);
                if sys4.attrib[8]=TL then
                      begin
                        Form1.StatusBar1.SimpleText:='Century '+IntToStr(pass)+
                            ': Processing LowTech Empire '+IntToStr(d);  {** V1.6 **}
                         {TL Rising new RP each pass???}
                         tladvance[d]:=0;
                         rp[d]:=rp[d]+de(10,1);
                         limit_cost:=tl_cost[TL]*trunc(5-sys4.attrib[4]/5);
                         if (TL=1) and (limit_cost<400) then limit_cost:=400;
                         if rp[d]>limit_cost then
                             begin
                                sys4.attrib[8]:=sys4.attrib[8]+1;
                                rp[d]:=0;
                                if TL=5 then sys4.attrib[15]:=pass+1;
                                seek (pfile,abs(sys4.pln_id));
                                read (pfile,sys2);
                                seek (cyfile,d);
                                read (cyfile,sys7);
                                if sys7.gov=26 then subj_world:=true else subj_world:=false;
                                social_raise (sys4,aux_str,subj_world);
                                colony_growth(sys4,pass,sys7,sys2.mine_ress,
                                   sys2,mfile,cyfile,subj_world); {Updating capital info}
                                seek(afile,d);
                                write(afile,sys4);
                                seek(cyfile,d);
                                write(cyfile,sys7);
                                if Form1.CheckBox1.Checked then writeln(tfile,'Civ   : Race ',d:4,
                                   ' TL Rising to ',sys4.attrib[8],' (New gov: ',gov[sys4.gov_type],aux_str,')');
                                if Form1.CheckBox8.Checked then writeln(evfile,pass,';Civ;',d,';;;;',
                                   'TL Rising to ',sys4.attrib[8],' (New gov: ',gov[sys4.gov_type],aux_str,')');
                             end;
                      end;
             end;
      end;

end;

{** CivGen High-Tech TL research **}
procedure main_hitech (var afile: alien_record_file; var tfile:textfile;
  var pfile: planet_record_file; var cyfile: colony_record_file; pass: byte;
  var evfile:textfile;var mfile:moon_record_file);
var TL: byte;
    d:word;
    limit_cost: smallint;
    sys4: alien_record;
    sys2: planet_record;
    sys7: colony_record;
const tl_cost: array [1..4] of word= (15,20,50,120);
begin
   for TL:=9 downto 6 do
       begin
         for d:=0 to (filesize(afile)-1) do
             begin
                seek(afile,d);
                read(afile,sys4);
                if (sys4.attrib[8]=TL) and (sys4.attrib[15]>=pass) then
                      begin
                        Form1.StatusBar1.SimpleText:='Century '+IntToStr(pass)+
                            ': Processing HiTech Empire '+IntToStr(d);  {** V1.6 **}
                         {TL Rising new RP each pass???}
                         if (sys4.attrib[8]-tladvance[d])<6 then
                            begin
                                seek (cyfile,d);
                                read (cyfile,sys7);
                                sys7.starport:=4;
                                seek (cyfile,d);
                                write (cyfile,sys7);
                            end;
                         if rp[d]=-1 then {** uplifting **}
                             begin
                                rp[d]:=0;
                                seek (pfile,abs(sys4.pln_id));
                                read (pfile,sys2);
                                seek (cyfile,d);
                                read (cyfile,sys7);
                                if sys7.gov=26 then subj_world:=true
                                   else subj_world:=false;
                                colony_growth(sys4,pass,sys7,sys2.mine_ress,
                                   sys2,mfile,cyfile,subj_world); {Updating capital info}
                                seek(afile,d);
                                write(afile,sys4);
                                seek(cyfile,d);
                                write(cyfile,sys7);
                             end;
                         tladvance[d]:=0;
                         rp[d]:=rp[d]+de(10,1);
                         if is_set(sys4.table_abil[9],2) then rp[d]:=rp[d]+1;
                         limit_cost:=tl_cost[TL-5]*trunc(5-sys4.attrib[4]/5);
                         if rp[d]>limit_cost then
                             begin
                                sys4.attrib[8]:=sys4.attrib[8]+1;
                                rp[d]:=0;
                                seek (pfile,abs(sys4.pln_id));
                                read (pfile,sys2);
                                seek (cyfile,d);
                                read (cyfile,sys7);
                                if sys7.gov=26 then subj_world:=true
                                   else subj_world:=false;
                                colony_growth(sys4,pass,sys7,sys2.mine_ress,
                                   sys2,mfile,cyfile,subj_world); {Updating capital info}
                                seek(afile,d);
                                write(afile,sys4);
                                seek(cyfile,d);
                                write(cyfile,sys7);
                                if Form1.CheckBox1.Checked then writeln(tfile,'Civ   : Race ',d:4,
                                   ' TL Rising to ',sys4.attrib[8]);
                                if Form1.CheckBox8.Checked then writeln(evfile,pass,';Civ;',d,
                                   ';;;;TL Rising to ',sys4.attrib[8]);
                             end;
                      end;
             end;
      end;

end;



{** CivGen random event proc **}
procedure event_calcul (var sys7:colony_record;var tfile:textfile;var cyfile:colony_record_file;
    pass:byte;colony_id:longint;var pfile:planet_record_file;var afile:alien_record_file;
    var evfile:textfile;var sys2:planet_record);
var event_type,aux,tmp,old_ice,res:byte;
    oldgnp,event_rand:word;
    system_id:longint;
    code:integer;
    event_factor:integer;
    sys4:alien_record;
    world_str:string[10];
    aux_str:string;
    old_energ_abs,new_energ_abs:single;
const max_event=34;
      {** Infertility should always be the last event **}
      event_genre: array[0..max_event+1] of string[30] =
          ('Birthrate soars','Natural catastrophe','Economic collapse',
           'Agricultural disaster','Plague','High rates of immigration',
           'TL collapse','Integration of natives','Civil unrest',
           'Research breakthrough','Civil rights progress','Major space accident',
           'Social trouble','World wide war','Crime Wave','Pirate raiding',
           'Ecological disaster','Restoring order','Major happy event',
           'Successful terrorist attempt','Nuclear world wide war','Religious crisis',
           'Mineral ressources crisis','Major religious event','Anti-science demonstrations',
           'Pro-science demonstrations','Xenophobic demonstrations',
           'Pro-alien demonstrations','Militarist demonstrations','Pacifist demonstrations',
           'Genetic manipulations','Infertile plague','Charismatic leader',
           'Economic boon','Terraformation','Infertility cured');
      gen_type: array[1..6] of byte =
           (9,12,71,82,86,91);
      event_home: array[1..40] of byte =
           (0,1,2,3,4,6,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
            32,33,34,1,2,16,3,4,9,8);
      event_colony: array[1..40] of byte =
           (0,1,2,3,4,5,6,7,8,10,11,12,13,14,15,16,17,18,19,20,22,23,32,33,34,
            0,1,2,3,4,5,6,7,8,10,11,12,13,14,15);

begin
     {Random events}
     val(Form1.Edit10.Text,event_factor,code);
     if (sys7.col_class=8) or (sys7.col_class=7) then event_rand:=50*event_factor
        else event_rand:=100*event_factor;
     if (sys7.body_type=1) and Form1.CheckBox3.Checked and (sys7.age>pass) then
         begin
           if de(event_rand,1)=1 then
              begin
                aux:=de(max_event+6,1);
                if (sys7.col_class=8) or (sys7.col_class=7) then
                   event_type:=event_home[aux]
                  else
                   event_type:=event_colony[aux];
                case event_type of
                  5 : if sys7.age=255 then event_type:=0;
                  7 : if ((sys7.race=sys7.allegiance) or (sys7.allegiance=65535))
                      then event_type:=10;
                  11: if sys7.starport=5 then event_type:=12;
                  13: if ((sys7.starport<5) or (sys7.pop<40)) then event_type:=12;
                  15: if sys7.starport=5 then event_type:=14;
                  16: if (sys7.starport=5) and (sys7.pop<45) then event_type:=2;
                  20: if sys7.starport=5 then event_type:=14;
                  22: if sys7.body_type=2 then event_type:=3;
                  34: if (sys2.world_type<>14) or (sys2.temp_avg<-25)
                         or (is_set(sys2.unusual[5],5)) then event_type:=33;
                end;
                aux_str:='';
                case event_type of
                        0    : if sys7.pop<50 then
                                  begin
                                      sys7.pop:=sys7.pop+1;
                                      if trunc(sys7.pop/10)<> trunc((sys7.pop-1)/10) then
                                         begin
                                           oldgnp:=sys7.gnp;
                                           sys7.gnp:=trunc(sys7.gnp*evm[trunc(sys7.pop/10)]/
                                              evm[trunc((sys7.pop-1)/10)]);
                                           mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]
                                               -sys7.power;
                                           sys7.power:=trunc(sys7.gnp*sys7.power/oldgnp);
                                           mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]
                                              +sys7.power;
                                         end;
                                  end;
                        11,12,15: if sys7.stability>2 then sys7.stability:=sys7.stability-1;
                        1,2,4,19: begin
                                    sys7.stability:=sys7.stability-2;
                                    if sys7.stability<1 then sys7.stability:=1;
                                  end;
                        3,8,13,16: begin
                                    sys7.stability:=sys7.stability-3;
                                    if sys7.stability<1 then sys7.stability:=1;
                                  end;
                        6    : begin
                                sys7.stability:=sys7.stability-4;
                                if sys7.stability<1 then sys7.stability:=1;
                                if sys7.col_class=8 then rp[sys7.race]:=0;
                               end;
                        5    : if sys7.pop<40 then
                                  begin
                                      sys7.pop:=sys7.pop+2;
                                      if trunc(sys7.pop/10)<> trunc((sys7.pop-2)/10) then
                                         begin
                                           oldgnp:=sys7.gnp;
                                           sys7.gnp:=trunc(sys7.gnp*evm[trunc(sys7.pop/10)]/
                                              evm[trunc((sys7.pop-2)/10)]);
                                           mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]
                                               -sys7.power;
                                           sys7.power:=trunc(sys7.gnp*sys7.power/oldgnp);
                                           mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]
                                              +sys7.power;
                                         end;
                                  end;
                        7,10   : begin
                                   if sys7.stability<10 then sys7.stability:=sys7.stability+1;
                                   if sys7.law>1 then sys7.law:=sys7.law-1;
                                 end;
                        9      : begin
                                   if rp[sys7.race]<200 then rp[sys7.race]:=rp[sys7.race]+10
                                      else rp[sys7.race]:=rp[sys7.race]+30;
                                 end;
                        14     : begin
                                    sys7.stability:=sys7.stability-2;
                                    if sys7.stability<1 then sys7.stability:=1;
                                    sys7.crime:=sys7.crime+2;
                                    if sys7.crime>10 then sys7.crime:=10;
                                  end;
                        17     : begin
                                    sys7.stability:=sys7.stability+2;
                                    if sys7.stability>10 then sys7.stability:=10;
                                    if sys7.law<9 then sys7.law:=sys7.law+2;
                                    sys7.crime:=sys7.crime-2;
                                    if sys7.crime<1 then sys7.crime:=1;
                                  end;
                        18     : if sys7.stability<10 then sys7.stability:=sys7.stability+1;
                        20     : begin
                                    sys7.stability:=sys7.stability-3;
                                    if sys7.stability<1 then sys7.stability:=1;
                                    sys7.crime:=sys7.crime+2;
                                    if sys7.crime>10 then sys7.crime:=10;
                                    if sys7.body_type=1 then
                                       begin
                                         set_bit(sys2.unusual[1],2);
                                         seek(pfile,sys7.world_id);
                                         write(pfile,sys2);
                                       end;
                                 end;
                        21     : begin
                                    sys7.stability:=sys7.stability-3;
                                    if sys7.stability<1 then sys7.stability:=1;
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    aux_str:=' ('+religion_genre[sys4.religion];
                                    case sys4.attrib[8] of
                                      1,2:sys4.religion:=de(5,1);
                                      3..10: sys4.religion:=de(8,1)+2;
                                    end;
                                    if (sys4.religion=4) and (de(3,1)=1) then sys4.religion:=11;
                                    if (sys4.gov_type=7) and (sys4.religion>6) then sys4.religion:=10;
                                    case sys4.religion of
                                       8,9: sys4.devotion:=0;
                                       else begin
                                              if sys4.gov_type=7 then sys4.devotion:=de(5,1)+5
                                              else sys4.devotion:=de(10,1);
                                            end;
                                    end;
                                    aux_str:=aux_str+' to '+religion_genre[sys4.religion]+')';
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                 end;
                        22    : begin
                                    for tmp:=1 to 5 do
                                       begin
                                          sys2.mine_ress[tmp]:=round(sys2.mine_ress[tmp]/2);
                                          if sys2.mine_ress[tmp]=0 then sys2.mine_ress[tmp]:=1;
                                       end;
                                    seek(pfile,sys7.world_id);
                                    write(pfile,sys2);
                                    sys7.stability:=sys7.stability-2;
                                    if sys7.stability<1 then sys7.stability:=1;
                                end;
                        23    : begin
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys7.stability:=sys7.stability+trunc(sys4.devotion/3);
                                    if sys7.stability>10 then sys7.stability:=10;
                                end;
                        24    : begin
                                    sys7.stability:=sys7.stability-2;
                                    if sys7.stability<1 then sys7.stability:=1;
                                    rp[sys7.race]:=trunc(rp[sys7.race]/2);
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[4]:=round(sys4.attrib[4]*0.75);
                                    if sys4.attrib[4]<1 then sys4.attrib[4]:=1;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        25    : begin
                                    sys7.stability:=sys7.stability+1;
                                    if sys7.stability>10 then sys7.stability:=10;
                                    rp[sys7.race]:=rp[sys7.race]+de(5,1);
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[4]:=sys4.attrib[4]+3;
                                    if sys4.attrib[4]>20 then sys4.attrib[4]:=20;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        26    : begin
                                    sys7.stability:=sys7.stability-1;
                                    if sys7.stability<1 then sys7.stability:=1;
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[3]:=round(sys4.attrib[3]*0.75);
                                    if sys4.attrib[3]<1 then sys4.attrib[3]:=1;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        27    : begin
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[3]:=sys4.attrib[3]+3;
                                    if sys4.attrib[3]>20 then sys4.attrib[3]:=20;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        28    : begin
                                    sys7.stability:=sys7.stability+1;
                                    if sys7.stability>10 then sys7.stability:=10;
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[1]:=sys4.attrib[1]+3;
                                    if sys4.attrib[1]>20 then sys4.attrib[1]:=20;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        29    : begin
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    sys4.attrib[1]:=round(sys4.attrib[1]*0.75);
                                    if sys4.attrib[1]<1 then sys4.attrib[1]:=1;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        30    : begin
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    aux:=gen_type[de(6,1)];
                                    tmp:=1+trunc((aux-1)/8);
                                    set_bit(sys4.table_abil[tmp],aux-(tmp-1)*8);
                                    aux_str:='('+special_ability[aux]+')';
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        31    : begin
                                    seek(afile,sys7.race);
                                    read(afile,sys4);
                                    if is_set(sys4.table_abil[5],8) then
                                       begin
                                         unset_bit(sys4.table_abil[5],8);
                                         event_type:=max_event+1;
                                         sys7.stability:=sys7.stability+2;
                                         if sys7.stability>10 then sys7.stability:=10;
                                       end
                                      else
                                       begin
                                         set_bit(sys4.table_abil[5],8);
                                         unset_bit(sys4.table_abil[11],1);
                                         sys7.stability:=sys7.stability-2;
                                         if sys7.stability<1 then sys7.stability:=1;
                                       end;
                                    seek(afile,sys7.race);
                                    write(afile,sys4);
                                end;
                        32,33 : begin
                                     sys7.stability:=sys7.stability+2;
                                     if sys7.stability>10 then sys7.stability:=10;
                                end;
                        34    : begin
                                    sys7.stability:=sys7.stability+1;
                                    if sys7.stability>10 then sys7.stability:=10;
                                    old_energ_abs:=power((1-(sys2.hydrography[4]/100))/0.7,
                                       0.25);
                                    sys2.hydrography[4]:=sys2.hydrography[4]-5;
                                    new_energ_abs:=power((1-(sys2.hydrography[4]/100))/0.7,
                                       0.25);
                                    sys2.temp_avg:=trunc((sys2.temp_avg+273)*new_energ_abs
                                       /old_energ_abs)-273;
                                    old_ice:=sys2.hydrography[2];
                                    sys2.hydrography[2]:=Ice_fraction(sys2.hydrography[1],
                                       sys2.temp_avg);
                                    sys2.hydrography[1]:=sys2.hydrography[1]+old_ice-
                                       sys2.hydrography[2];
                                    set_bit(sys2.unusual[5],5);
                                    seek(pfile,sys7.world_id);
                                    write(pfile,sys2);
                                end;
                end;
                if Form1.CheckBox1.Checked then
                   begin
                      if sys7.allegiance<>65535 then writeln(tfile,'Event : ',event_genre[event_type],aux_str,' on planet ',sys7.world_id,
                       ' (Race ',sys7.allegiance,')')
                         else writeln(tfile,'Event : ',event_genre[event_type],' on planet ',sys7.world_id,
                           ' (Independant)');
                   end;
                if Form1.CheckBox8.Checked then
                   begin
                     case sys7.body_type of
                       1: begin
                            system_id:=sys2.sun_id;
                            world_str:=IntToStr(sys7.world_id);
                          end;
                       2: begin
                            system_id:=sys2.sun_id;
                            world_str:='M '+IntToStr(sys7.world_id);
                          end;
                      end;
                      if sys7.allegiance<>65535 then writeln(evfile,pass,';Event;',sys7.allegiance,';;',world_str,';',
                           system_id,';',event_genre[event_type],aux_str)
                        else  writeln(evfile,pass,';Event;Indep.',';;',world_str,';',system_id,';',event_genre[event_type]);
                   end;
                seek(cyfile,colony_id);
                write(cyfile,sys7);
              end;
         end;
end;


procedure main_empire;
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4,a_temp: alien_record;
    sys5: names_record;
    sys7: colony_record;
    sys8,sys8a: contact_record;
    sfile: star_record_file;
    afile,oafile: alien_record_file;
    nfile: name_record_file;
    pfile: planet_record_file;
    mfile: moon_record_file;
    cyfile: colony_record_file;
    ctfile: contact_record_file;
    tfile,csvfile,evfile: textfile;
    X,Y,Z: integer;
    TL,pass,max_pass,old_gov,old_TL: byte;
    b:longint;
    code,posrecord:integer;
    temp_var:smallint;
    change_rel:shortint;
    dist,spread_dist,factor,spread_min,spread_max:single;
    aux,aux1,aux2,aux3,aux_str: string;
    a:word;
    c,d:longint;
begin
    for a:=0 to 65535 do
       begin
         mi_power[a]:=0;
         homeplanet[a]:=-1;
         max_dist[a]:=3;
         alliance[a]:=false;
         tladvance[a]:=0; {CivGen variable}
         rp_diplo[a]:=0; {CivGen variable, 1 new War diplo this century, 2 trade}
         rp[a]:=0;
       end;
    {Check if races begin at TL=1}
    if Form1.CheckBox6.Checked then civ_factor:=true else civ_factor:=false;

    val(Form1.Edit2.Text,dist_table[1],code);
    if code<>0 then dist_table[1]:=1;
    val(Form1.Edit3.Text,dist_table[2],code);
    if code<>0 then dist_table[2]:=2;
    val(Form1.Edit4.Text,dist_table[3],code);
    if code<>0 then dist_table[3]:=3;
    val(Form1.Edit5.Text,dist_table[4],code);
    if code<>0 then dist_table[4]:=5;
    val(Form1.Edit6.Text,dist_table[5],code);
    if code<>0 then dist_table[5]:=4;
    val(Form1.Edit7.Text,victory_value,code);
    if (code<>0) or (victory_value<1.25) then
       begin
         victory_value:=1.25;
         Form1.Edit7.Text:='1.25';
       end;
    val(Form1.Edit8.Text,mibonus,code);
    if (code<>0) or (mibonus<0) then
       begin
         mibonus:=0;
         Form1.Edit8.Text:='0';
       end;
    if Form1.CheckBox5.Checked then max_range:=2 else max_range:=50;

    assign (sfile,concat(namefile,'.sun'));
    assign (afile,concat(namefile,'.aln'));
    assign (pfile,concat(namefile,'.pln'));
    assign (cyfile,concat(namefile,'.col'));
    assign (ctfile,concat(namefile,'.con'));
    assign (nfile,concat(namefile,'.nam'));
    assign (mfile,concat(namefile,'.mon'));
    assign (tfile,concat(namefile,'.his'));
    assign (csvfile,concat(namefile,'_RP.csv'));
    assign (evfile,concat(namefile,'.csv'));

    if Form1.CheckBox7.checked then
       begin
         rewrite(csvfile);
         writeln(csvfile,'Century;Race;RP');
       end;

    if Form1.CheckBox8.checked then
       begin
         rewrite(evfile);
         writeln(evfile,'Century;Type;Race1;Race2;Planet;System;Event');
       end;

    reset(nfile);
    read(nfile,sys5);
    closefile(nfile);
    if (sys5.body_type=0) and (sys5.body_Id<8) then
      begin
        Form1.StatusBar1.SimpleText:='Updating datas to Star v1.6';
        update_data(namefile,sys5.body_Id);
        sys5.body_Id:=8;
        reset(nfile);
        seek(nfile,0);
        write(nfile,sys5);
        closefile(nfile);
        Form1.StatusBar1.SimpleText:='Update done';
      end;

    Form1.StatusBar1.SimpleText:='Initialization';
    reset(nfile);
    reset(afile);
    reset(sfile);
    reset(pfile);
    reset(mfile);

    habit_table:=VarArrayCreate([0,filesize(pfile)-1],VarByte);
    diplo_table:=VarArrayCreate([0,filesize(afile)-1,0,filesize(afile)-1],VarByte);
    file_table:=VarArrayCreate([0,filesize(afile)-1,0,filesize(afile)-1],VarInteger);

    for c:=0 to (filesize(pfile)-1) do habit_table[c]:=255;
    for c:=0 to (filesize(afile)-1) do
        for d:=0 to (filesize(afile)-1) do
           begin
             diplo_table[c,d]:=0;
             file_table[c,d]:=0;
           end;

    assign (oafile,concat(namefile,'.aln1'));
    rewrite(oafile);
    for c:=0 to (filesize(afile)-1) do
       begin
          seek(afile,c);
          read(afile,sys4);
          write(oafile,sys4);
       end;
    closefile(oafile);

    if FileExists(concat(namefile,'.con')) then
       begin
         reset(ctfile);
         if filesize(ctfile)>0 then
            begin
              for c:=0 to (filesize(ctfile)-1) do
                begin
                  seek (ctfile,c);
                  read(ctfile,sys8);
                  diplo_table[sys8.empire1,sys8.empire2]:=sys8.relation;
                  diplo_table[sys8.empire2,sys8.empire1]:=sys8.relation;
                end;
            end;
       end
     else rewrite(ctfile);
    rewrite(cyfile);
    rewrite(tfile);

    Init_system (afile,sfile,pfile,cyfile,mfile,tfile,evfile);

    max_pass:=0;
    if not civ_factor then
       for d:=0 to (filesize(afile)-1) do
          begin
             seek (afile,d);
             read(afile,sys4);
             if sys4.attrib[15]>max_pass then max_pass:=sys4.attrib[15];
          end;
    if civ_factor then
       begin
         val(Form1.Edit9.Text,max_pass,code);
         if max_pass<80 then
            begin
                 max_pass:=80;
                 Form1.Edit9.Text:='80';
            end;
         if max_pass>130 then
            begin
                 max_pass:=130;
                 Form1.Edit9.Text:='130';
            end;
       end;

    randomize;
    stardll_ini;
    Form1.StatusBar1.SimpleText:='Initialization done';

    for pass:=(max_pass-1) downto 0 do
      begin
        Application.ProcessMessages; {** v1.6 refreshing window ** }
        str(pass,aux);
        aux:=aux+' centuries ago:';
        if Form1.CheckBox1.Checked then writeln(tfile,aux);

        for d:=0 to (filesize(afile)-1) do
          begin
            case rp_diplo[a] of
              1: rp[a]:=rp[a]+de(6,1);
              2: rp[a]:=rp[a]+de(3,1);
            end;
            rp_diplo[a]:=0;
          end;

        main_hitech(afile,tfile,pfile,cyfile,pass,evfile,mfile);
        main_lowtech(afile,tfile,pfile,cyfile,pass,evfile,mfile);   {** Low tech pass **}

        {** High TL pass -- empire std **}
        for TL:= 10 downto 6 do
          begin
             for d:=0 to (filesize(afile)-1) do
                begin
                   seek(afile,d);
                   read(afile,sys4);
                   if (sys4.attrib[8]=TL) and (sys4.attrib[15]>pass) then
                      begin
                        Form1.StatusBar1.SimpleText:='Century '+IntToStr(pass)+
                            ': Colonizing stars, Empire '+IntToStr(d);  {** V1.6 **}
                        tladvance[d]:=0;
                        seek(pfile,abs(sys4.pln_id));
                        read(pfile,sys2);
                        seek(sfile,sys2.sun_id);
                        read(sfile,sys1);
                        X:=sys1.posX;
                        Y:=sys1.posY;
                        Z:=sys1.posZ;
                        spread_dist:=dist_table[sys4.attrib[8]-5];
                        factor:=(sys4.attrib[4]*3+sys4.attrib[2]+sys4.attrib[3])/60;
                        if factor<0.5 then factor:=0.5;
                        spread_dist:=spread_dist*factor;
                        if sys4.attrib[15]=0 then sys4.attrib[15]:=1; {Bug with TL6 races  Star v1.56a-}
                        spread_min:=(sys4.attrib[15]-pass-1)*spread_dist;
                        if (spread_min>0) and (not Form1.CheckBox2.Checked) then
                           spread_min:=0;
                        spread_max:=(sys4.attrib[15]-pass)*spread_dist;
                        for b:=0 to (filesize(sfile)-1) do
                            begin
                               seek(sfile,b);
                               read(sfile,sys1);
                               dist:=sqrt(sqr(sys1.posX-X)+sqr(sys1.posY-Y)
                                     +sqr(sys1.posZ-Z));
                               if (dist=0) and (sys4.pln_id<0) and (sys4.attrib[15]=pass+1) then
                                  begin
                                     if Form1.CheckBox1.Checked then
                                        writeln(tfile,'Info  : Extra sector Race ',d,' founds a colony on planet ',
                                        abs(sys4.pln_id),' (System ',b,')');
                                     if Form1.CheckBox8.Checked then
                                        writeln(evfile,pass,';Info;',d,';;',abs(sys4.pln_id),';',b,
                                        ';Colony founded by extra sector race');
                                     init_colonize_system(d,sys1,sys4,ctfile,afile,
                                        pfile,cyfile,sfile,mfile,pass,tfile,b,evfile);
                                     seek(sfile,b);
                                     write(sfile,sys1);
                                  end;
                               if (dist>=spread_min) and (dist<=spread_max) and
                                  (dist<=(max_dist[d]+max_range)) then
                                  begin
                                     init_colonize_system(d,sys1,sys4,ctfile,afile,
                                        pfile,cyfile,sfile,mfile,pass,tfile,b,evfile);
                                     seek(sfile,b);
                                     write(sfile,sys1);
                                  end;
                            end;
                      end;
                end;
          end;

        {Updating War conquest & Event}
        {if sys2.allegiance<>sys7.race}
        for d:=0 to (filesize(cyfile)-1) do
          begin
             seek(cyfile,d);
             read(cyfile,sys7);
             case sys7.body_type of
               1:begin
                  seek(pfile,sys7.world_id);
                  read(pfile,sys2);
                 end;
               2:begin
                  seek(mfile,sys7.world_id);
                  read(mfile,sys3);
                  seek(pfile,sys3.pln_id);
                  read(pfile,sys2);
                 end;
             end;

             {Random events}
             event_calcul (sys7,tfile,cyfile,pass,d,pfile,afile,evfile,sys2);

             {Updating TL advance on economic and power rating due to TL uplifting due to
              new contact, TL advance due to relation change has not been yet coded}
             if (tladvance[sys7.race]>0) and (sys7.race=sys7.allegiance) then {TL benefits for owned worlds only}
                  begin
                     seek(afile,sys7.race);
                     read(afile,sys4);
                     mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]-sys7.power;
                     sys7.gnp:=trunc(sys7.gnp*sys4.attrib[8]/(sys4.attrib[8]-tladvance[sys7.race]));
                     sys7.power:=trunc(sys7.gnp*(sys4.attrib[1]+21-sys4.attrib[3])*0.0125
                        *mdm[sys4.attrib[8]]);
                     if sys7.col_class=10 then sys7.power:=trunc(sys7.power*1.5);
                     mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
                  end;

             if (sys2.allegiance<>sys7.allegiance) and (sys7.allegiance<>65535) then
                begin
                  mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]-sys7.power;
                  {conquered Military bases have their alien race changed to
                   their conqueror}
                  if sys7.col_class=10 then sys7.race:=sys2.allegiance
                    else begin
                            if diplo_table[sys7.race,sys2.allegiance]>3 then
                               begin
                                  allied_free(pfile,sys2,sys7,tfile,evfile,pass,afile);
                               end
                              else
                               begin
                                  sys7.allegiance:=sys2.allegiance;
                                  sys7.gov:=26; {Subjugated gov}
                                  if sys7.law<9 then sys7.law:=sys7.law+2;
                                  if sys7.stability>2 then sys7.stability:=sys7.stability-1;
                               end;
                         end;
                  if sys7.gov=26 then
                     begin
                       if sys7.pop_comp>94 then sys7.pop_comp:=sys7.pop_comp-de(20,1);
                       if ((sys7.col_class=7) or (sys7.col_class=8)) and (sys7.pop_comp<85)
                          then sys7.pop_comp:=85;
                       if sys7.starport=5 then sys7.starport:=4;
                       if (not is_set(sys7.misc_char[1],1)) and (sys7.pop>25) then
                          begin
                            if sys7.pop>40 then c:=de(15,1)
                               else c:=de(30,1);
                            if c<sys7.law then set_bit(sys7.misc_char[1],1); {Military Base}
                          end;
                       if (sys7.starport<3) and (not is_set(sys7.misc_char[1],2)) and (sys7.pop>25) and
                          (de(6,2)>9) then set_bit(sys7.misc_char[1],2); {Naval Base}
                       if (sys7.pop<40) and (sys7.crime>5) and (sys7.law>5) and (de(50,1)=1)
                          then set_bit(sys7.misc_char[1],3); {Prison Camp}
                       if (sys7.pop<40) and (de(200,1)=1) then set_bit(sys7.misc_char[1],4); {Exile Camp}
                     end;
                  mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
                  seek(cyfile,d);
                  write(cyfile,sys7);
                end;
             {Update secession worlds
              in CivGen probability has been raised to 3-stability instead of 1}
             if (sys7.stability<3) and (de(sys7.law+15,1)<=(3-sys7.stability)) and (sys7.body_type=1) then
                begin
                   if sys7.gov=26 then
                      begin
                         if (sys7.col_class=7) or (sys7.col_class=8) then
                            begin
                                 seek(afile,sys7.race);
                                 read(afile,sys4);
                                 sys7.gov:=sys4.gov_type;
                                 if sys7.pop_comp<90 then sys7.pop_comp:=sys7.pop_comp+5;
                                 if Form1.CheckBox1.Checked then
                                   writeln(tfile,'Event : Capital Planet ',sys7.world_id,' (Race ',sys7.race,') get independance from race ',
                                   sys7.allegiance);
                                 if Form1.CheckBox8.Checked then
                                   writeln(evfile,pass,';Event;',sys7.race,';',sys7.allegiance,';',sys7.world_id,';',
                                     sys2.sun_id,';Capital Planet get independance from race ',sys7.allegiance);
                            end
                           else
                            begin
                                 sys7.gov:=27; {Free Subjugated world}
                                 if sys7.pop_comp<80 then sys7.pop_comp:=sys7.pop_comp+10;
                                 if Form1.CheckBox1.Checked then
                                   writeln(tfile,'Event : Planet ',sys7.world_id,' has acquired independance from race ',
                                   sys7.allegiance);
                                 if Form1.CheckBox8.Checked then
                                   writeln(evfile,pass,';Event;',sys7.race,';',sys7.allegiance,';',sys7.world_id,';',
                                     sys2.sun_id,';Planet get independance from race ',sys7.allegiance);
                            end;
                         mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]-sys7.power;
                         sys7.allegiance:=sys7.race;
                         seek(pfile,sys7.world_id);
                         read(pfile,sys2);
                         sys2.allegiance:=sys7.race;
                         seek(pfile,sys7.world_id);
                         write(pfile,sys2);
                         mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
                      end
                   else
                      begin
                         old_gov:=sys7.gov;
                         if sys7.race<>65535 then  {To fix an unknown bug}
                            begin
                               seek(afile,sys7.race);
                               read(afile,sys4);
                               case sys4.attrib[8] of
                                  1,2:sys7.gov:=de(7,1);
                                  3..6: sys7.gov:=de(10,1)+4;
                                  7..10: sys7.gov:=de(8,1)+6;
                               end;
                             end
                           else sys7.gov:=de(10,1)+4;
                         if sys7.gov=9 then
                           begin
                              case de(20,1) of
                                1,2:sys7.gov:=15;
                                3,4:sys7.gov:=16;
                                5,6:sys7.gov:=17;
                                7,8:sys7.gov:=18;
                                9,10:sys7.gov:=19;
                                11:sys7.gov:=20;
                                12:sys7.gov:=25;
                              end;
                           end;
                           if (sys7.col_class=7) or (sys7.col_class=8) then
                                begin
                                  if Form1.CheckBox1.Checked then
                                    writeln(tfile,'Event : ',gov[old_gov],' government has been overthrown by ',
                                      gov[sys7.gov],' on planet ',sys7.world_id,' (Race ',sys7.race,')');
                                  if Form1.CheckBox8.Checked then
                                    writeln(evfile,pass,';Event;',sys7.race,';;',sys7.world_id,';',sys2.sun_id,';',gov[old_gov],
                                    ' government has been overthrown by ',gov[sys7.gov]);
                                  seek(afile,sys7.race);
                                  read(afile,sys4);
                                  sys4.gov_type:=sys7.gov;
                                  seek(afile,sys7.race);
                                  write(afile,sys4);
                                end
                              else
                                begin
                                   if Form1.CheckBox1.Checked then
                                      writeln(tfile,'Event : Planet ',sys7.world_id,' has acquired independance (Race ',
                                        sys7.race,') (Govt:',gov[sys7.gov],')');
                                   if Form1.CheckBox8.Checked then
                                      writeln(evfile,pass,';Event;',sys7.race,';;',sys7.world_id,
                                      ';',sys2.sun_id,';Planet has acquired independance (Govt:',gov[sys7.gov],')');
                                   mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]-sys7.power;
                                   sys7.allegiance:=65535; {Independant world}
                                end;
                      end;
                   case sys7.gov of
                      13: sys7.law:=de(5,1)+1;
                      12: sys7.law:=de(5,1)+5;
                      else sys7.law:=de(9,1)+1;
                   end;
                   sys7.stability:=de(7,1)+3;
                   temp_var:=de(8,1)+2-trunc(sys7.law/3)-trunc(sys7.stability/3);
                   if temp_var<1 then temp_var:=1;
                   sys7.crime:=temp_var;
                   seek(cyfile,d);
                   write(cyfile,sys7);
                end;

          end;

        {Updating diplomatic relations}
        if Form1.CheckBox3.Checked then
          for d:=0 to (filesize(ctfile)-1) do
            begin
               if de(100,1)=1 then
                 begin
                   seek(ctfile,d);
                   read(ctfile,sys8);
                   case sys8.relation of
                      1  : change_rel:=+1;
                      2,3: if de(2,1)=1 then change_rel:=+1
                           else change_rel:=-1;
                      4  : if de(2,1)=1 then change_rel:=-1
                            else if de(2,1)=1 then change_rel:=+1
                               else change_rel:=0;
                      5  : if de(4,1)>1 then change_rel:=0
                               else change_rel:=-1;
                   end;
                   sys8.relation:=sys8.relation+change_rel;
                   diplo_table[sys8.empire1,sys8.empire2]:=sys8.relation;
                   diplo_table[sys8.empire2,sys8.empire1]:=sys8.relation;
                   if (Form1.CheckBox1.Checked) and (change_rel<>0) then
                      writeln(tfile,'Diplo : Diplomatic relations changed to ',
                        relation_genre[sys8.relation],' between ',sys8.empire1,' and ',
                        sys8.empire2);
                   if (Form1.CheckBox8.Checked) and (change_rel<>0) then
                      writeln(evfile,pass,';Diplo;',sys8.empire1,';',sys8.empire2,
                        ';;;Diplomatic relations changed to ',relation_genre[sys8.relation]);
                   if sys8.relation=1 then
                      begin
                         rp_diplo[sys8.empire1]:=1;
                         rp_diplo[sys8.empire2]:=1;
                      end;
                   if sys8.relation=5 then
                      begin
                        seek(afile,sys8.empire1);
                        read(afile,sys4);
                        seek(afile,sys8.empire2);
                        read(afile,a_temp);
                        if (a_temp.attrib[8]>5) and (a_temp.attrib[15]<(pass+1))
                                then begin  {Spatial age raised }
                                        a_temp.attrib[15]:=pass+1;
                                        seek(afile,sys8.empire2);
                                        write(afile,a_temp);
                                     end;
                        if sys4.attrib[8]>a_temp.attrib[8] then
                           begin
                             if Form1.CheckBox1.Checked then
                                 write(tfile,'        TL uplifting from ',a_temp.attrib[8],' to ');
                             if Form1.CheckBox8.Checked then
                                 write(evfile,pass,';Civ;',sys8.empire2,';;;;','TL uplifting from ',a_temp.attrib[8],' to ');
                             old_TL:=a_temp.attrib[8];
                             a_temp.attrib[8]:=sys4.attrib[8];
                             tl_uplifting(sys8.empire2,a_temp,sys8); {New proc}
                             if old_TL<6 then
                                begin
                                  social_raise (a_temp,aux_str,false);
                                  aux_str:=' (New gov : '+gov[a_temp.gov_type]+aux_str+')';
                                end;  
                             seek(afile,sys8.empire2);
                             write(afile,a_temp);
                             if Form1.CheckBox1.Checked then writeln(tfile,a_temp.attrib[8],aux_str);
                             if Form1.CheckBox8.Checked then writeln(evfile,a_temp.attrib[8],aux_str);
                            end
                        else if a_temp.attrib[8]>sys4.attrib[8] then
                           begin
                             if Form1.CheckBox1.Checked then
                                 write(tfile,'        TL uplifting from ',sys4.attrib[8],' to ');
                             if Form1.CheckBox8.Checked then
                                 write(evfile,pass,';Civ;',sys8.empire1,';;;;','TL uplifting from ',sys4.attrib[8],' to ');
                             old_TL:=sys4.attrib[8];
                             sys4.attrib[8]:=a_temp.attrib[8];
                             tl_uplifting(sys8.empire1,sys4,sys8); {New proc}
                             if old_TL<6 then
                                begin
                                  social_raise (sys4,aux_str,false);
                                  aux_str:=' (New gov : '+gov[sys4.gov_type]+aux_str+')';
                                end;  
                             seek(afile,sys8.empire1);
                             write(afile,sys4);
                             if Form1.CheckBox1.Checked then writeln(tfile,sys4.attrib[8],aux_str);
                             if Form1.CheckBox8.Checked then writeln(evfile,sys4.attrib[8],aux_str);
                            end;

                       {check unity with other allied races}
                       for c:=0 to (filesize(afile)-1) do
                          begin
                            if (diplo_table[sys8.empire1,c]=5) and (c<>sys8.empire2)
                              and (diplo_table[sys8.empire2,c]<>5) then
                               begin
                                  if file_table[sys8.empire2,c]<>0 then
                                     begin
                                        posrecord:=file_table[sys8.empire2,c];
                                        seek(ctfile,posrecord);
                                        read(ctfile,sys8a);
                                        sys8a.relation:=5;
                                        seek(ctfile,posrecord);
                                        write(ctfile,sys8a);
                                        diplo_table[sys8.empire2,c]:=5;
                                        diplo_table[c,sys8.empire2]:=5;
                                        if Form1.CheckBox1.Checked then
                                           writeln(tfile,'Diplo : Diplomatic relations changed to Unity between ',
                                             sys8a.empire1,' and ',sys8a.empire2);
                                        if Form1.CheckBox8.Checked then
                                           writeln(evfile,pass,';Diplo',sys8a.empire1,';',sys8a.empire2,
                                           ';;;Diplomatic relations changed to Unity');
                                     end
                                    else
                                     begin {New contact}
                                        sys8a.empire1:=sys8.empire2;
                                        sys8a.empire2:=c;
                                        sys8a.age:=pass;
                                        sys8a.relation:=5;
                                        diplo_table[sys8.empire2,c]:=5;
                                        diplo_table[c,sys8.empire2]:=5;
                                        file_table[sys8.empire2,c]:=filesize(ctfile);
                                        file_table[c,sys8.empire2]:=filesize(ctfile);
                                        seek(ctfile,filesize(ctfile));
                                        write(ctfile,sys8a);
                                        if Form1.CheckBox1.Checked then
                                           writeln(tfile,'Diplo : New diplomatic Unity relations between ',
                                             sys8a.empire1,' and ',sys8a.empire2);
                                        if Form1.CheckBox8.Checked then
                                           writeln(evfile,pass,';Diplo;',sys8a.empire1,';',sys8a.empire2,
                                           ';;;New diplomatic Unity relations');
                                     end;
                               end;
                            if (diplo_table[sys8.empire2,c]=5) and (c<>sys8.empire1)
                              and (diplo_table[sys8.empire1,c]<>5) then
                               begin
                                  if file_table[sys8.empire1,c]<>0 then
                                     begin
                                        posrecord:=file_table[sys8.empire1,c];
                                        seek(ctfile,posrecord);
                                        read(ctfile,sys8a);
                                        sys8a.relation:=5;
                                        seek(ctfile,posrecord);
                                        write(ctfile,sys8a);
                                        diplo_table[sys8.empire1,c]:=5;
                                        diplo_table[c,sys8.empire1]:=5;
                                        if Form1.CheckBox1.Checked then
                                           writeln(tfile,'Diplo : Diplomatic relations changed to Unity between ',
                                             sys8a.empire1,' and ',sys8a.empire2);
                                        if Form1.CheckBox8.Checked then
                                           writeln(evfile,pass,';Diplo;',sys8a.empire1,';',sys8a.empire2,
                                           ';;;Diplomatic relations changed to Unity');
                                     end
                                    else
                                     begin {New contact}
                                        sys8a.empire1:=sys8.empire1;
                                        sys8a.empire2:=c;
                                        sys8a.age:=pass;
                                        sys8a.relation:=5;
                                        diplo_table[sys8.empire1,c]:=5;
                                        diplo_table[c,sys8.empire1]:=5;
                                        file_table[sys8.empire1,c]:=filesize(ctfile);
                                        file_table[c,sys8.empire1]:=filesize(ctfile);
                                        seek(ctfile,filesize(ctfile));
                                        write(ctfile,sys8a);
                                        if Form1.CheckBox1.Checked then
                                           writeln(tfile,'Diplo : New diplomatic Unity relations between ',
                                             sys8a.empire1,' and ',sys8a.empire2);
                                        if Form1.CheckBox8.Checked then
                                           writeln(evfile,pass,';Diplo;',sys8a.empire1,';',sys8a.empire2,
                                           ';;;New diplomatic Unity relations');
                                     end;
                               end;
                          end;
                      end;
                   if sys8.relation=4 then
                      begin
                        seek(afile,sys8.empire1);
                        read(afile,sys4);
                        seek(afile,sys8.empire2);
                        read(afile,a_temp);
                        if sys4.attrib[8]>(a_temp.attrib[8]+1) then
                           begin
                             if Form1.CheckBox1.Checked then
                                 write(tfile,'        TL uplifting from ',a_temp.attrib[8],' to ');
                             if Form1.CheckBox8.Checked then
                                 write(evfile,pass,';Civ;',sys8.empire2,';;;;','TL uplifting from ',a_temp.attrib[8],' to ');
                             old_TL:=a_temp.attrib[8];
                             a_temp.attrib[8]:=sys4.attrib[8]-1;
                             tl_uplifting(sys8.empire2,a_temp,sys8); {New proc}
                             if old_TL<6 then
                                begin
                                  social_raise (a_temp,aux_str,false);
                                  aux_str:=' (New gov : '+gov[a_temp.gov_type]+aux_str+')';
                                end;
                             seek(afile,sys8.empire2);
                             write(afile,a_temp);
                             if Form1.CheckBox1.Checked then writeln(tfile,a_temp.attrib[8],aux_str);
                             if Form1.CheckBox8.Checked then writeln(evfile,a_temp.attrib[8],aux_str);
                           end;
                        if a_temp.attrib[8]>(sys4.attrib[8]+1) then
                           begin
                             if Form1.CheckBox1.Checked then
                                 write(tfile,'        TL uplifting from ',sys4.attrib[8],' to ');
                             if Form1.CheckBox8.Checked then
                                 write(evfile,pass,';Civ;',sys8.empire1,';;;;','TL uplifting from ',sys4.attrib[8],' to ');
                             old_TL:=sys4.attrib[8];
                             sys4.attrib[8]:=a_temp.attrib[8]-1;
                             tl_uplifting(sys8.empire1,sys4,sys8); {New proc}
                             if old_TL<6 then
                                begin
                                  social_raise (sys4,aux_str,false);
                                  aux_str:=' (New gov : '+gov[sys4.gov_type]+aux_str+')';
                                end;
                             seek(afile,sys8.empire1);
                             write(afile,sys4);
                             if Form1.CheckBox1.Checked then writeln(tfile,sys4.attrib[8],aux_str);
                             if Form1.CheckBox8.Checked then writeln(evfile,sys4.attrib[8],aux_str);
                           end;
                      end;
                   seek(ctfile,d);
                   write(ctfile,sys8);
                 end;
            end;

        {Updating RP log file}
        if Form1.CheckBox7.Checked then
           begin
             for d:=0 to (filesize(afile)-1) do
                 writeln(csvfile,pass,';',d,';',RP[d]);
           end;

        if Form1.CheckBox1.Checked then writeln(tfile);
      end;

    Form1.StatusBar1.SimpleText:='Updating economy datas';  
    for d:=0 to (filesize(cyfile)-1) do
      begin
        seek(cyfile,d);
        read(cyfile,sys7);
        if sys7.starport<5 then
          begin
            colony_eco(sys7);
            seek(cyfile,d);
            write(cyfile,sys7);
          end;
      end;
    closefile(nfile);
    closefile(afile);
    closefile(sfile);
    closefile(mfile);
    closefile(pfile);
    closefile(ctfile);
    closefile(cyfile);
    closefile(tfile);
    if Form1.CheckBox7.Checked then closefile (csvfile);
    if Form1.CheckBox8.Checked then closefile (evfile);
    save_empire(cyfile,ctfile,afile);
end;

procedure TForm1.Button1Click(Sender: TObject);
var  a:byte;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        namefile:=OpenDialog1.FileName;
        delete(namefile,a-3,4);
      end;
   Edit1.Text:=namefile;
end;


procedure TForm1.Button2Click(Sender: TObject);
begin
  Halt(1);
end;

procedure TForm1.FormCreate(Sender: TObject);
var a:byte;
    current_dir,helpdir: string;
begin
   OpenDialog1.InitialDir:='.\data';
   if OpenDialog1.Execute then
     begin
       a:=length(OpenDialog1.FileName);
       namefile:=OpenDialog1.FileName;
       delete(namefile,a-3,4);
     end;
   OpenDialog1.InitialDir:='..\data';
   Edit2.Text:='1';
   Edit3.Text:='2';
   Edit4.Text:='3';
   Edit5.Text:='5';
   Edit6.Text:='4';
   GetDir(0,current_dir);
   helpdir:=concat(current_dir,'..\star.hlp');
   Edit1.Text:=namefile;
   Application.HelpFile:=helpdir;
end;


procedure TForm1.Button3Click(Sender: TObject);
var cyfile:file of colony_record;
    cy_size,code,event_factor: integer;
    auxstr:string;
    time_start,time_end:TDateTime;
begin
  val(Form1.Edit10.Text,event_factor,code);
  if event_factor<1 then event_factor:=1;
  if event_factor>10 then event_factor:=10;
  Str(trunc(event_factor),auxstr);
  Form1.Edit10.Text:=auxstr;
  if FileExists (concat(namefile,'.col')) then
     begin
       assignfile (cyfile,concat(namefile,'.col'));
       reset (cyfile);
       cy_size:=FileSize(cyfile);
       closefile(cyfile);
     end
     else cy_size:=0;
  if cy_size=0 then
     begin
       time_start:=Time;
       Screen.Cursor:=crHourglass;
       main_empire;
       Screen.Cursor:=crdefault;
       time_end:=Time;
       Form1.StatusBar1.SimpleText:='CivGen finished at '+TimeToStr(Time)+
          '  (Running time: '+TimeToStr(time_end-time_start)+')';
     end
    else
     begin
       ShowMessage('Empire has already been run with this sector');
     end;
end;



procedure TForm1.Button5Click(Sender: TObject);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    afile,oafile: alien_record_file;
    sfile: star_record_file;
    mfile: moon_record_file;
    pfile: planet_record_file;
    cyfile: colony_record_file;
    ctfile: contact_record_file;
    efile: empire_record_file;
    a:longint;
begin
    assignfile (sfile,concat(namefile,'.sun'));
    assignfile (pfile,concat(namefile,'.pln'));
    assignfile (cyfile,concat(namefile,'.col'));
    assignfile (ctfile,concat(namefile,'.con'));
    assignfile (mfile,concat(namefile,'.mon'));
    assignfile (efile,concat(namefile,'.emp'));
    rewrite(efile);
    closefile(efile);
    reset(sfile);
    reset(pfile);
    reset(mfile);
    Screen.Cursor:=crHourglass;

    Form1.StatusBar1.SimpleText:='Cleaning CivGen infos';
    if FileExists(concat(namefile,'.aln1')) then  {** v1.6 Get back old alien file **}
       begin
         assignfile (afile,concat(namefile,'.aln'));
         assignfile (oafile,concat(namefile,'.aln1'));
         rewrite(afile);
         reset(oafile);
         for a:=0 to (filesize(oafile)-1) do
             begin
                  seek(oafile,a);
                  read(oafile,sys4);
                  write(afile,sys4);
             end;
         closefile(oafile);
         closefile(afile);
         DeleteFile(concat(namefile,'.aln1'));
       end;
    for a:=0 to (filesize(sfile)-1) do
       begin
          read(sfile,sys1);
          sys1.allegiance:=65535;
          sys1.code:=0;
          seek(sfile,a);
          write(sfile,sys1);
       end;
    for a:=0 to (filesize(pfile)-1) do
       begin
          read(pfile,sys2);
          sys2.allegiance:=65535;
          unset_bit(sys2.misc_charac,4);
          unset_bit(sys2.unusual[5],4); {**v1.57c added**}
          unset_bit(sys2.unusual[4],5); {**v1.6 Clean recent ruins**}
          unset_bit(sys2.unusual[4],7); {**v1.6 Clean space cemetery**}
          unset_bit(sys2.unusual[4],8); {**v1.6 Clean wonder**}
          unset_bit(sys2.unusual[5],1); {**v1.6 Clean holy site**}
          unset_bit(sys2.unusual[5],5); {**v1.6 Clean terraformed**}
          seek(pfile,a);
          write(pfile,sys2);
       end;
    for a:=0 to (filesize(mfile)-1) do
       begin
          read(mfile,sys3);
          unset_bit(sys3.misc_charac,4);
          seek(mfile,a);
          write(mfile,sys3);
       end;

    rewrite(ctfile);
    rewrite(cyfile);
    closefile(ctfile);
    closefile(mfile);
    closefile(cyfile);
    closefile(sfile);
    closefile(pfile);
    Form1.StatusBar1.SimpleText:='CivGen infos cleared';
    Screen.Cursor:=crdefault;
    ShowMessage('CivGen infos have been cleared');
end;


procedure TForm1.Button6Click(Sender: TObject);
begin
Form2.Edit1.Clear;
Form2.Edit2.Text:='0';
Form2.Edit3.Text:='0';
Form2.Edit4.Text:='0';
Form2.Show;
end;

procedure TForm1.Button4Click(Sender: TObject);
var a:byte;
begin
  Form3.ComboBox1.Items.Clear;
  for a:=1 to 5 do Form3.ComboBox1.Items.Add(relation_genre[a]);
  Form3.ComboBox1.ItemIndex:=0;
  Form3.Show;
end;

end.
