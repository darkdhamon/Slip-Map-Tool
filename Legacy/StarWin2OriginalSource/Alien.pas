{ *******************************************************************
  Alien Generator for Windows v1.6 by Aina Rasolomalala (c) 03-2001

  v1.41 : Change in the Spatial Age attribute determination
  v1.42 : 32-bit version
  v1.43 : the average size has been raised
  v1.44 : max spatial age parameter
  v1.57b: new biology (appearance_calcul,body_proc,ability_calcul),
          new diets (Solar, Thermal, Parasite)
  v1.6  : new appearance (radial), new cover (cellulose), new body part
          (jelly bag)
}

unit Alien;

interface

procedure main_alien (temperature:smallint;gravity:single;atmos_comp,world_type:
    byte;new_file:boolean;outstr:string;mother_id:longint;
    al_disable:boolean;max_spatial:byte);export;
procedure alien_read_ini_files;export;

implementation

uses recunit,starunit,procunit,IniFiles,sysUtils;

type table_ini6=array[1..6] of byte;

var alien_prob: table_ini6;

function max_range(int1,max1:byte):byte;
begin
   if int1>max1 then max_range:=max1
      else max_range:=int1;
end;

function min_range(int1,min1:byte):byte;
begin
   if int1<min1 then min_range:=min1
      else min_range:=int1;
end;

procedure charac(var sys4:alien_record; atmos:byte;max_spatial:byte);
var a:byte;
    aux:shortint;
begin
  for a:=1 to 7 do sys4.attrib[a]:=de(18,1)+2;
  case sys4.app_genre of
      2,21: begin
           sys4.attrib[2]:=max_range(sys4.attrib[2]+1,20);
           sys4.attrib[5]:=max_range(sys4.attrib[5]+2,20);
           sys4.attrib[6]:=max_range(sys4.attrib[6]+2,20);
           sys4.attrib[10]:=sys4.attrib[10]-2;
         end;
      4: sys4.attrib[5]:=max_range(sys4.attrib[5]+1,20);
      5: begin
           sys4.attrib[1]:=max_range(sys4.attrib[1]+2,20);
           sys4.attrib[6]:=sys4.attrib[6]-1;
           sys4.attrib[5]:=sys4.attrib[5]-1;
         end;
  end;
  case sys4.mass of
      1..10: sys4.attrib[9]:=de(3,1);
      11..25:  sys4.attrib[9]:=de(3,1)+2;
      26..50: sys4.attrib[9]:=de(3,1)+5;
      51..70: sys4.attrib[9]:=de(4,1)+7;
      71..90: sys4.attrib[9]:=de(4,1)+8;
      91..125: sys4.attrib[9]:=de(4,1)+9;
      126..150: sys4.attrib[9]:=de(4,1)+11;
      151..200: sys4.attrib[9]:=de(4,1)+13;
      201..300: sys4.attrib[9]:=de(4,1)+15;
      301..500: sys4.attrib[9]:=de(4,1)+17;
      501..1000: sys4.attrib[9]:=de(6,1)+20;
  end;
  case (de(1000,1)+sys4.attrib[4]*7+sys4.attrib[2]*2+sys4.attrib[6]) of
       1..300  : sys4.attrib[8]:=1;
       301..500: sys4.attrib[8]:=2;
       501..640: sys4.attrib[8]:=3;
       641..770: sys4.attrib[8]:=4;
       771..870: sys4.attrib[8]:=5;
       871..970: sys4.attrib[8]:=6;
       971..1060: sys4.attrib[8]:=7;
       1061..1180: sys4.attrib[8]:=8;
       1181..1195: sys4.attrib[8]:=9;
       else sys4.attrib[8]:=10;
  end;
  if (atmos=19) and (sys4.attrib[8]>2) then sys4.attrib[8]:=de(2,1);
  sys4.attrib[10]:=sys4.attrib[8]+de(4,4)-4;
  sys4.attrib[11]:=de(3,3)+5;
  case (de(60,1)+sys4.attrib[8]*5) of
       1..10 : sys4.attrib[12]:=de(5,1)+1;
       11..25: sys4.attrib[12]:=de(10,1)+2;
       26..45: sys4.attrib[12]:=de(15,1)+4;
       46..70: sys4.attrib[12]:=de(30,1)+8;
       71..80: sys4.attrib[12]:=de(60,1)+12;
       81..90: sys4.attrib[12]:=de(100,1)+25;
       else sys4.attrib[12]:=de(200,1)+50;
  end;
  if sys4.environment_type=5 then sys4.attrib[11]:=sys4.attrib[11]+3;
  if (sys4.environment_type<>5) and (sys4.mass<10) then
     sys4.attrib[11]:=sys4.attrib[11]-2;
  sys4.attrib[13]:=de(25,1)-round(sys4.attrib[4]/2)+10;
  if (sys4.attrib[13]-10)<1 then sys4.attrib[13]:=1
     else sys4.attrib[13]:=sys4.attrib[13]-10;
  if sys4.attrib[13]>20 then sys4.attrib[13]:=20;
  aux:=de(12,1)+round((40-sys4.attrib[6]-sys4.attrib[5])/5);
  case sys4.app_genre of
     2: aux:=aux-1;
     5: aux:=aux+1;
     18: aux:=aux-2;
  end;
  if aux<1 then aux:=1;
  if aux>20 then aux:=20;
  sys4.attrib[14]:=aux;
  aux:=0;
  case sys4.attrib[8] of
     6: aux:=de(3,1)+round((20-sys4.attrib[4])/5);
     7: aux:=de(10,1)+round((20-sys4.attrib[4])/4);
     8: aux:=de(6,5)+round((20-sys4.attrib[4])/3);
     9: aux:=de(10,5)+round((20-sys4.attrib[4])/2)+5;
     10: aux:=de(100,1)+30-sys4.attrib[4];
  end;
  sys4.attrib[15]:=aux;
  if sys4.attrib[15]>max_spatial then sys4.attrib[15]:=max_spatial;
end;

procedure appearance_calcul(var sys4:alien_record;fins,wings:boolean;
          atmos_comp:byte;al_disable:boolean);
var legs_nbr,dual_legs_nbr,a,arms_nbr,tent_nbr:byte;
begin
  if al_disable then sys4.app_genre:=12 else sys4.app_genre:=13;
  case sys4.body_cover_type of
	   1,2: begin
		case de(12,1) of
		    1..6 :sys4.app_genre:=1;
		    7..10:if fins then sys4.app_genre:=11
			    else sys4.app_genre:=1;
                    11   :if sys4.environment_type=3 then sys4.app_genre:=11
                            else sys4.app_genre:=22;
                    12   : if al_disable and fins then sys4.app_genre:=11
                            else if al_disable then sys4.app_genre:=1;
		end;
                if sys4.limbs_number=0 then sys4.app_genre:=15;
	      end;
	   3: begin
		case de(12,1) of
		    1,2:sys4.app_genre:=4;
		    3,4:sys4.app_genre:=5;
		    5:sys4.app_genre:=6;
		    6:sys4.app_genre:=7;
                    7:sys4.app_genre:=23;
		    8..10:sys4.app_genre:=12;
                    11:if al_disable then sys4.app_genre:=6;
                    12: if al_disable then sys4.app_genre:=7;
		end;
	       end;
	    4: begin
		 case de(12,1) of
		   1..2:sys4.app_genre:=10;
		   3..9:if wings then sys4.app_genre:=10;
                   else if al_disable then sys4.app_genre:=10;
		 end;
	       end;
	    5: begin
                 if de(12,1)>2 then sys4.app_genre:=3;
                 if (sys4.limbs_number=0) and (sys4.app_genre=3) then
                   begin
                     sys4.app_genre:=16;
                     sys4.limbs_number:=1;
                   end;
               end;
	    6: begin
		 case de (12,1) of
		    1:sys4.app_genre:=8;
		    2:sys4.app_genre:=3;
		    3,4:sys4.app_genre:=12;
                    5,6: if al_disable then sys4.app_genre:=8;
                    7,8: if al_disable then sys4.app_genre:=3;
                    else if al_disable then sys4.app_genre:=10;
		 end;
	       end;
	    7:
	       begin
		 case de (12,1) of
		   1    :sys4.app_genre:=9;
                   2..10: if (sys4.environment_type=3) or
                         (sys4.environment_type=4) then sys4.app_genre:=20
                         else sys4.app_genre:=2;
                   11   : if al_disable then sys4.app_genre:=9;
                   12   : if al_disable then sys4.app_genre:=2;
		 end;
	       end;
            8: begin
                 case de (20,1) of
                   1..19: sys4.app_genre:=9;
                   20: if al_disable then sys4.app_genre:=2;
                 end;
               end;
            9: begin
                 a:=de(10,1);
                 case a of
                      1,2:  begin               {Geometric Form}
                            sys4.app_genre:=19;
                            sys4.body_type:=4;
                          end;
                      3,4:  begin
                            sys4.app_genre:=17;  {Energetic Form}
                            sys4.body_type:=9;
                          end;
                      5,6:  begin
                            sys4.body_type:=5;   {Liquid Form}
                            sys4.app_genre:=24;
                          end;
                      7,8: begin
                            sys4.body_type:=8;   {Gaseous Form}
                            sys4.app_genre:=25;
                          end;
                 end;
                end;
            10: sys4.app_genre:=18;
            12: sys4.app_genre:=8;
  end;
  if sys4.body_type=5 then sys4.app_genre:=24;
  case sys4.app_genre of  {Liquid , energy and Gaseous forms doesn't have limbs}
     17,24,25: sys4.limbs_number:=0;
  end;
  if fins and (sys4.body_cover_type>3) and (sys4.app_genre<>3)
     and (sys4.app_genre<>12) and (not al_disable) then sys4.app_genre:=13;
  if (sys4.diet_genre=4) and (de(10,1)>4) and (sys4.body_cover_type<4) then
     begin
       sys4.app_genre:=8;
       sys4.diet_genre:=6;
     end;
  arms_nbr:=0;
  legs_nbr:=0;
  tent_nbr:=0;
  dual_legs_nbr:=0;
  for a:=1 to sys4.limbs_number do
      begin
        if sys4.limbs_genre[a]>4 then arms_nbr:=arms_nbr+1;
        if sys4.limbs_genre[a]=3 then legs_nbr:=legs_nbr+1;
        if sys4.limbs_genre[a]=4 then dual_legs_nbr:=dual_legs_nbr+1;
        if sys4.limbs_genre[a]=6 then tent_nbr:=tent_nbr+1;
      end;
  if (legs_nbr=2) and (sys4.environment_type=1) and (sys4.limbs_genre[1]=5)
     then
     begin
       case sys4.app_genre of
         1: if de(3,1)>1 then sys4.app_genre:=14;
         3,4,5,6,7,8,9,10,12,22,23: if de(3,1)=1 then sys4.app_genre:=14;
       end;
     end;
  if wings and (arms_nbr=0) and (sys4.body_cover_type<6) then sys4.app_genre:=10;
  if (legs_nbr>2) and (sys4.app_genre<>2) and (sys4.app_genre<>13) and (not al_disable)
     then sys4.app_genre:=13;
  if ((legs_nbr+dual_legs_nbr)>2) and (sys4.app_genre=2) and (de(3,1)>1) then
     sys4.app_genre:=21;
  if (sys4.limbs_number>3) and (sys4.body_cover_type<6) and (sys4.body_cover_type<>4)
     and (de(3,1)=1) then
         sys4.app_genre:=21;
  if (sys4.limbs_number=tent_nbr) and (sys4.limbs_number>0) then
     begin
        sys4.app_genre:=26;
        if (sys4.environment_type=3) or (sys4.environment_type=4) then
           set_bit(sys4.body_char[2],1);
     end;
  if (atmos_comp<>2) or (sys4.body_type<>1) then
     begin
         case sys4.app_genre of
           1..7,10,11,12,14,16: if not al_disable then sys4.app_genre:=13;
           8 : if ((atmos_comp=4) or (atmos_comp=12)  or (atmos_comp=15) or
               (atmos_comp=19)) and (not al_disable) then sys4.app_genre:=13;
         end;
     end;
  if (sys4.app_genre=15) and (sys4.environment_type=2) then
     sys4.environment_type:=1;
  case sys4.body_type of
     2,6,7,8,9: sys4.diet_genre:=6;
     5        : if de(5,1)>1 then sys4.diet_genre:=7;
     3        : if de(3,1)=1 then sys4.diet_genre:=7;
  end;
  if (sys4.body_type>3) and (de(20,1)=1) then sys4.diet_genre:=8;
  if (sys4.diet_genre=3) and (sys4.size_creat<30) and (de(5,1)=1) then sys4.diet_genre:=5;
  case sys4.app_genre of
     1,4,5,6,7,12,14,22,23: if sys4.repro_meth_genre=2 then
                               sys4.repro_meth_genre:=3;
     3,10,11,16,20        : if sys4.repro_meth_genre=3 then
                               sys4.repro_meth_genre:=2;
     2,21                 : begin
                               if sys4.repro_meth_genre=3 then
                                  sys4.repro_meth_genre:=2;
                               if de(5,1)=1 then sys4.repro_genre:=5;   
                            end;
  end;
  if (sys4.app_genre=15) and (sys4.limbs_number=0) then
     begin
        sys4.limbs_number:=de(5,1)-1;
        for a:=1 to sys4.limbs_number do sys4.limbs_genre[a]:=7;
     end;
end;

procedure size_calcul(gravity:single;var sys4:alien_record);
begin
  case de(100,1) of
	 1..5 : begin
		  sys4.mass:=de(10,1)+1;
		  sys4.size_creat:=de(50,1)+10;
		 end;
	 6..15: begin
		  sys4.mass:=de(30,1)+10;
		  sys4.size_creat:=de(70,1)+50;
		 end;
	 16..85: begin
		  sys4.mass:=de(70,1)+50;
		  sys4.size_creat:=de(180,1)+100;
		 end;
	 86..95: begin
		  sys4.mass:=de(200,1)+80;
		  sys4.size_creat:=de(300,1)+125;
		 end;
	 96..100:begin
		  sys4.mass:=de(750,1)+250;
		  sys4.size_creat:=de(500,1)+250;
		 end;
  end;
  if gravity <0.8 then
       begin
	    sys4.size_creat:=round(sys4.size_creat*1.2);
	    sys4.mass:=round(sys4.mass*0.9);
       end;
  if gravity >1.3 then
       begin
	    sys4.size_creat:=round(sys4.size_creat*0.9);
	    sys4.mass:=round(sys4.mass*1.1);
       end;
end;


procedure ability_calcul(gravity:single;temperature:smallint;var fins,wings,
    warm_blood:boolean;atmos_comp:byte;var sys4:alien_record;al_disable:boolean;
    var blind:boolean);
var nbr_spec_abil,a,weapon_chance,tmp,aux:byte;
    nipper_chance,fangs_chance,claw_chance,part_chance:shortint;

begin
  blind:=true;
  if atmos_comp=2 then nbr_spec_abil:=de(2,3)-3
     else nbr_spec_abil:=de(4,1);
  for a:= 1 to nbr_spec_abil do
      begin
        aux:=de(max_abilities-4,1);
        if aux>50 then aux:=aux+4;  { 51-54 slots can't be given randomly}
        tmp:=1+trunc((aux-1)/8);
        set_bit(sys4.table_abil[tmp],aux-(tmp-1)*8);
      end;
  appearance_calcul(sys4,fins,wings,atmos_comp,al_disable);
  if temperature<-5 then
	begin
	  set_bit(sys4.table_abil[2],1);
	  unset_bit(sys4.table_abil[2],4);
	  unset_bit(sys4.table_abil[1],8);
	  if de(100,1)<51 then set_bit(sys4.table_abil[2],3);
	end;
  if temperature>30 then
	begin
	  set_bit(sys4.table_abil[2],4);
	  unset_bit(sys4.table_abil[2],1);
	  if de(100,1)<51 then set_bit(sys4.table_abil[1],8);
	end;
  if is_set(sys4.table_abil[3],2) then unset_bit(sys4.table_abil[1],1);
  if is_set(sys4.table_abil[1],1) then unset_bit(sys4.table_abil[1],2);
  if is_set(sys4.table_abil[1],4) then
      begin
        unset_bit(sys4.table_abil[1],5);
        blind:=false;
      end;
  if is_set(sys4.table_abil[1],5) then blind:=false;
  if is_set(sys4.table_abil[2],4) then unset_bit(sys4.table_abil[2],3);
  if is_set(sys4.table_abil[2],1) then unset_bit(sys4.table_abil[1],8);
  if is_set(sys4.table_abil[2],2) then blind:=false;
  if is_set(sys4.table_abil[2],5) then blind:=false;
  if is_set(sys4.table_abil[2],6) then blind:=false;
  if is_set(sys4.table_abil[3],4) and ((sys4.app_genre<>13) and  {Web spinning}
     (sys4.app_genre<>2)) then unset_bit(sys4.table_abil[3],4);
  if is_set(sys4.table_abil[3],3) and (de(10,1)>4) then     {Wall climbing}
     unset_bit(sys4.table_abil[3],3);
  if is_set(sys4.table_abil[3],5) then blind:=false;        {Nictatin membrane}
  if is_set(sys4.table_abil[3],6) and (de(25,1)>1) then     {Radio Hearing}
     unset_bit(sys4.table_abil[3],6);
  if is_set(sys4.table_abil[3],7) and (de(10,1)>4) then
     unset_bit(sys4.table_abil[3],7);
  if is_set(sys4.table_abil[3],8) and (de(15,1)>1) then      {Metamorphosis}
     unset_bit(sys4.table_abil[3],8);
  if is_set(sys4.table_abil[4],1) and (de(10,1)>3) then
     unset_bit(sys4.table_abil[4],1);
  if is_set(sys4.table_abil[4],2) and (de(10,1)>5) then
     unset_bit(sys4.table_abil[4],2);
  if is_set(sys4.table_abil[4],4) and (de(10,1)>3) then    {Dampen}
     unset_bit(sys4.table_abil[4],4);
  if is_set(sys4.table_abil[4],5) and (de(10,1)>3) then
     unset_bit(sys4.table_abil[4],5);
  if is_set(sys4.table_abil[4],5) then blind:=false;
  if is_set(sys4.table_abil[4],7) and (de(10,1)>2) then    {Vampirism}
     unset_bit(sys4.table_abil[4],7);
  if is_set(sys4.table_abil[4],8) then  {Slow Motion}
      begin
        case sys4.app_genre of
          9,13: if de(10,1)>1 then unset_bit(sys4.table_abil[4],8);
          else  if de(30,1)>1 then unset_bit(sys4.table_abil[4],8);
        end;
      end;
  if (is_set(sys4.table_abil[5],1) and (de(100,1)>1)) or
     (sys4.environment_type<>5) then unset_bit(sys4.table_abil[5],1);
  if is_set(sys4.table_abil[5],2) and (de(20,1)>1) then
     unset_bit(sys4.table_abil[5],2);
  if is_set(sys4.table_abil[6],3) and (de(20,1)>1) then
     unset_bit(sys4.table_abil[6],3);
  if is_set(sys4.table_abil[5],4) and (de(10,1)>1) then
     unset_bit(sys4.table_abil[5],4);
  if (is_set(sys4.table_abil[5],5) and (de(10,1)>1)) or    {Mystical power}
     (sys4.attrib[8]<4) then unset_bit(sys4.table_abil[5],5);
  if is_set(sys4.table_abil[5],3) and ((de(30,1)>1) or     {Stretching}
     ((sys4.body_cover_type<>1) and (sys4.body_cover_type<>9)))
     then unset_bit(sys4.table_abil[5],3);
  if (sys4.body_type=5) or (sys4.body_type>7) then  set_bit(sys4.table_abil[5],3);
  if (is_set(sys4.table_abil[5],6) and (de(20,1)>1)) {Independent eyes}
     or (is_set(sys4.eye_char[1],1)) then
     unset_bit(sys4.table_abil[5],6);
  if is_set(sys4.table_abil[5],6) then blind:=false;
  if is_set(sys4.table_abil[6],2) and (sys4.attrib[10]<16) then  {Bicephalous}
     unset_bit(sys4.table_abil[6],2);
  if is_set(sys4.table_abil[6],1) and (de(30,1)>1) then
     unset_bit(sys4.table_abil[6],1);
  if is_set(sys4.table_abil[6],4) and (de(30,1)>1) then
     unset_bit(sys4.table_abil[6],4);
  if is_set(sys4.table_abil[6],5) and (de(20,1)>1) or   {Universal digestion}
     ((sys4.diet_genre<>2) and (sys4.diet_genre<>4)) then
     unset_bit(sys4.table_abil[6],5);
  if (is_set(sys4.table_abil[6],6) and (de(30,1)>1)) or   {Pressure support}
     (sys4.body_cover_type<7) then
     unset_bit(sys4.table_abil[6],6);
  if is_set(sys4.table_abil[6],7) then blind:=false;
  if is_set(sys4.table_abil[7],1) and (sys4.attrib[3]<14) then {Cultural adaptability}
     unset_bit(sys4.table_abil[7],1);
  if wings and (sys4.limbs_number>0) then set_bit(sys4.table_abil[7],4); {Winged flight}
  if (sys4.environment_type=5) and (sys4.limbs_number=0)
     then set_bit(sys4.table_abil[7],6);                {Flight}
  if (sys4.environment_type=3) or (sys4.environment_type=4)
     then set_bit(sys4.table_abil[7],5);
  if is_set(sys4.table_abil[7],8) and (de(40,1)>1) then  {Spectrum vision}
     unset_bit(sys4.table_abil[7],8);
  if is_set(sys4.table_abil[7],8) then blind:=false;
  if is_set(sys4.table_abil[8],1) and (de(20,1)>1) then  {Ultra sonic hearing}
     unset_bit(sys4.table_abil[8],1);
  if (is_set(sys4.table_abil[8],2) and (de(20,1)>1)) or is_set(sys4.table_abil[1],4)
     then unset_bit(sys4.table_abil[8],1);               {Microscopic vision}
  if is_set(sys4.table_abil[8],2) then blind:=false;
  if is_set(sys4.table_abil[8],3) and not blind {Blind}
     then unset_bit(sys4.table_abil[8],3);
  if is_set(sys4.table_abil[8],4) and (is_set(sys4.table_abil[1],1) or  {Deafness}
     is_set(sys4.table_abil[1],2) or (de(20,1)>1)) then unset_bit(sys4.table_abil[8],4);
  if is_set(sys4.table_abil[8],7) and ((sys4.attrib[8]<5) or (sys4.attrib[4]<11))
     then unset_bit(sys4.table_abil[8],7); {Engineer bonus}
  if is_set(sys4.table_abil[8],8) and (sys4.attrib[8]<5) then {Pilot bonus}
     unset_bit(sys4.table_abil[8],8);
  if is_set(sys4.table_abil[9],1) and (sys4.attrib[1]<12) then {Combat bonus}
     unset_bit(sys4.table_abil[9],1);
  if is_set(sys4.table_abil[9],2) and ((sys4.attrib[8]<6) or (sys4.attrib[4]<11))
     then unset_bit(sys4.table_abil[9],2);                     {Science bonus}
  if is_set(sys4.table_abil[9],4) and is_set(sys4.table_abil[8],3) {Light sensitivity}
     then unset_bit(sys4.table_abil[9],4);
  if is_set(sys4.table_abil[9],5) and (de(30,1)>1) then  {Involuntary dampen}
     unset_bit(sys4.table_abil[9],5);
  if is_set(sys4.table_abil[9],6) and is_set(sys4.table_abil[8],4) {Sound sensitivity}
     then unset_bit(sys4.table_abil[9],6);
  if is_set(sys4.table_abil[9],7) and (de(25,1)>1) then  {Disease tolerance}
     unset_bit(sys4.table_abil[9],7);
  if is_set(sys4.table_abil[9],8) and (sys4.attrib[10]<15) then {Eidetic memory}
     unset_bit(sys4.table_abil[9],8);
  if is_set(sys4.table_abil[10],2) and (de(10,1)>1) then  {No sense of smell/taste}
     unset_bit(sys4.table_abil[10],2);
  if is_set(sys4.table_abil[10],4) and (sys4.limbs_number=0) then  {High manual dexterity}
     unset_bit(sys4.table_abil[10],4);
  if is_set(sys4.table_abil[10],5) and (de(10,1)>1) then  {Perfect balance}
     unset_bit(sys4.table_abil[10],5);
  if is_set(sys4.table_abil[10],6) and (de(5,1)>1) then  {Foul odor}
     unset_bit(sys4.table_abil[10],6);
  if is_set(sys4.table_abil[10],7) and (de(20,1)>1) then  {Skin Color change}
     unset_bit(sys4.table_abil[10],7);
  if is_set(sys4.table_abil[5],8) then unset_bit(sys4.table_abil[11],1); {High fecundity}
  case sys4.app_genre of
    1,11: if is_set(sys4.table_abil[11],1) then unset_bit(sys4.table_abil[11],1);
  end;
  if is_set(sys4.table_abil[11],2) and (sys4.attrib[8]<7) then {Cybernetic Enh}
      unset_bit(sys4.table_abil[11],2);
  if (sys4.app_genre=18) and (sys4.attrib[8]>5) then set_bit(sys4.table_abil[11],2);
  if is_set(sys4.table_abil[11],3) and (sys4.attrib[8]<6) then {Computer skill}
      unset_bit(sys4.table_abil[11],3);
  if is_set(sys4.table_abil[11],5) and (de(10,1)>1) then       {Vibration sense}
     unset_bit(sys4.table_abil[11],5);
  if is_set(sys4.table_abil[12],1) and (de(20,1)>1) then       {Extra Heart}
     unset_bit(sys4.table_abil[12],1);
  if is_set(sys4.table_abil[12],2) and (de(10,1)>1) then       {Heavy Sleeper}
     unset_bit(sys4.table_abil[12],2);
  if is_set(sys4.table_abil[12],3) and (sys4.app_genre<>18) and (de(20,1)>1) then  {Light sleeper}
     unset_bit(sys4.table_abil[12],3);
  if is_set(sys4.table_abil[12],3) and is_set(sys4.table_abil[12],2) then
     unset_bit(sys4.table_abil[12],3);
  if is_set(sys4.table_abil[12],4) and (de(25,1)>1) then      {Chemical communication}
     unset_bit(sys4.table_abil[12],4);
  if is_set(sys4.table_abil[12],5) and ((sys4.attrib[10]<11) or (de(15,1)>1))then  {Lightning calculator}
     unset_bit(sys4.table_abil[12],5);
  if is_set(sys4.table_abil[12],6) and (de(10,1)>1) then       {Time sense}
     unset_bit(sys4.table_abil[12],6);
  if is_set(sys4.table_abil[12],7) and ((de(50,1)>1) or blind) then  {EM Imaging}
     unset_bit(sys4.table_abil[12],7);
  if is_set(sys4.table_abil[12],8) and (de(30,1)>1) then       {Ultrasonic comm}
     unset_bit(sys4.table_abil[12],8);
  if is_set(sys4.table_abil[12],8) and (not is_set(sys4.table_abil[8],1)) then {Ultrasonic comm => Ultrasonic hearing}
     set_bit(sys4.table_abil[8],1);
  if sys4.app_genre=17 then set_bit(sys4.table_abil[4],1);
  if (not warm_blood) and (sys4.body_type=1) then
      begin
         set_bit(sys4.table_abil[7],3);
         unset_bit(sys4.table_abil[2],1);
      end;
  if is_set(sys4.table_abil[6],7) and is_set(sys4.eye_char[1],1) then
     unset_bit(sys4.table_abil[6],7);
  if not is_set(sys4.table_abil[8],3) then blind:=false;
  {---Fangs,Claws,Nippers---}
  case sys4.diet_genre of
      1: weapon_chance:=5;
      2: weapon_chance:=10;
      3: weapon_chance:=20;
      4..8: weapon_chance:=5;
  end;
  case sys4.app_genre of
      5 :fangs_chance:=5;
      2,3,7,13,16,21,22 :fangs_chance:=0;
      else fangs_chance:=-5;
  end;
  case sys4.app_genre of
      5,7 :claw_chance:=5;
      2,3,4,10,13,16,21 :claw_chance:=0;
      else claw_chance:=-5;
  end;
  case sys4.app_genre of
      20 :nipper_chance:=5;
      2,13 :nipper_chance:=0;
      else nipper_chance:=-15;
  end;
  if sys4.limbs_number=0 then
     begin
       nipper_chance:=-20;
       claw_chance:=-20;
     end;
  if de(100,1)<=(weapon_chance+fangs_chance) then set_bit(sys4.body_char[1],5);
  if de(100,1)<=(weapon_chance+claw_chance) then set_bit(sys4.body_char[1],6);
  if de(100,1)<=(weapon_chance+nipper_chance) then set_bit(sys4.body_char[1],7);

  {---special body parts---}
  case sys4.app_genre of              {tails}
     3,4,5,7,12,23: part_chance:=7;
     13: part_chance:=5;
     11: part_chance:=3;
     else part_chance:=1;
  end;
  if de(100,1)<=part_chance then set_bit(sys4.body_char[1],1);
  case sys4.app_genre of              {trunks}
     12,13: part_chance:=3;
     else part_chance:=1;
  end;
  if de(100,1)<=part_chance then set_bit(sys4.body_char[1],2);
  case sys4.app_genre of              {horns}
     3,12: part_chance:=5;
     1,13: part_chance:=3;
     else part_chance:=1;
  end;
  if de(100,1)<=part_chance then set_bit(sys4.body_char[1],3);
  case sys4.app_genre of              {antennas}
     2,20,21,15: part_chance:=8;
     13: part_chance:=5;
     else part_chance:=1;
  end;
  if de(100,1)<=part_chance then set_bit(sys4.body_char[1],4);
  case sys4.app_genre of              {hooves}
     14: part_chance:=45;
     12,13,22: part_chance:=1;
     else part_chance:=0;
  end;
  if sys4.limbs_number=0 then part_chance:=0;
  if de(100,1)<=part_chance then set_bit(sys4.body_char[1],8);

  if is_set(sys4.table_abil[4],8) then
     begin
       sys4.attrib[11]:=0;
       if sys4.attrib[12]<100 then sys4.attrib[12]:=sys4.attrib[12]*2;
       if sys4.attrib[12]<10 then sys4.attrib[12]:=10;
     end;

end;

procedure gov_calcul(var sys4:alien_record);
begin
  case sys4.attrib[8] of
   1,2:sys4.gov_type:=de(7,1);
   3..6: sys4.gov_type:=de(10,1)+4;
   7..10: sys4.gov_type:=de(8,1)+6;
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
  case sys4.gov_type of
      3: begin
           sys4.attrib[6]:=sys4.attrib[6]+2;
           sys4.attrib[14]:=sys4.attrib[14]-2;
           if sys4.attrib[6]>20 then sys4.attrib[6]:=20;
           if sys4.attrib[14]<1 then sys4.attrib[14]:=1;
         end;
      21:begin
           sys4.attrib[14]:=sys4.attrib[14]-2;
           sys4.attrib[6]:=sys4.attrib[6]+3;
           if sys4.attrib[6]>20 then sys4.attrib[6]:=20;
           if sys4.attrib[14]<1 then sys4.attrib[14]:=1;
         end;
  end;
end;

procedure religion_determination(var sys4:alien_record);
begin
  case sys4.attrib[8] of
   1,2:sys4.religion:=de(5,1);
   3..10: sys4.religion:=de(8,1)+2;
  end;
  if (sys4.religion=4) and (de(3,1)=1) then sys4.religion:=11;
  if (sys4.gov_type=7) and (sys4.religion>6) then
      sys4.religion:=10;
  case sys4.religion of
      8,9: sys4.devotion:=0;
      else begin
             if sys4.gov_type=7 then sys4.devotion:=de(5,1)+5
                else sys4.devotion:=de(10,1);
           end;
  end;
end;


procedure alien_read_ini_files;
   var
        WinIni: TIniFile;
        exe_path: string;
   begin
        exe_path:=ExtractFilePath(ParamStr(0));
        WinIni := TIniFile.Create(concat(exe_path,'starwin.ini'));
        with WinIni do
        begin
           alien_prob[1]:=ReadInteger('Alien Probabilities', 'Human', 20);
           alien_prob[2]:=ReadInteger('Alien Probabilities', 'Animal', 30)+alien_prob[1];
           alien_prob[3]:=ReadInteger('Alien Probabilities', 'Reptile', 15)+alien_prob[2];
           alien_prob[4]:=ReadInteger('Alien Probabilities', 'Insect', 15)+alien_prob[3];
           alien_prob[5]:=ReadInteger('Alien Probabilities', 'Misc', 15)+alien_prob[4];
           alien_prob[6]:=ReadInteger('Alien Probabilities', 'Exotic', 5)+alien_prob[5];
        end;
        WinIni.Free;
   end;


procedure main_body(var sys4:alien_record;warm_blood:boolean);
var a,aux,main_appear:byte;
begin
aux:=de(100,1);
if warm_blood then aux:=de(80,1) else aux:=de(80,1)+20;
for a:=6 downto 1 do
   if aux<=alien_prob[a] then main_appear:=a;
case main_appear of
   1: begin
        if de(10,1)>1 then sys4.limbs_number:=2 else sys4.limbs_number:=3;
        sys4.body_type:=1;
        if de(10,1)>2 then sys4.body_cover_type:=1 else sys4.body_cover_type:=2;
      end;
   2: begin
        if de(10,1)>1 then sys4.limbs_number:=2 else sys4.limbs_number:=3;
        sys4.body_type:=1;
        case de(100,1) of
          1..30 : sys4.body_cover_type:=1;
          39..55: sys4.body_cover_type:=2;
          56..60: sys4.body_cover_type:=6;
          61..95: sys4.body_cover_type:=3
          else sys4.body_cover_type:=4;
        end;
      end;
   3: begin
        case de(10,1) of
          1: sys4.limbs_number:=0;
          2: sys4.limbs_number:=1;
          3..8: sys4.limbs_number:=2
          else sys4.limbs_number:=3;
        end;
        sys4.body_type:=1;
        sys4.body_cover_type:=5;
      end;
   4: begin
        case de(10,1) of
          1..4: sys4.limbs_number:=2;
          5..7: sys4.limbs_number:=3;
          8..9: sys4.limbs_number:=4;
          else sys4.limbs_number:=de(6,1)+4;
        end;
        sys4.body_type:=1;
        sys4.body_cover_type:=7;
      end;
   5: begin
        case de(100,1) of
            1..6   : sys4.limbs_number:=0;
            7..70  : sys4.limbs_number:=2;
	    71..90 : sys4.limbs_number:=3;
            91..98 : sys4.limbs_number:=4;
	    99..100: sys4.limbs_number:=(de(6,1)+4);
        end;
        case de(100,1) of
            1..20  : begin
                        sys4.body_cover_type:=12;
                        sys4.body_type:=1;
                     end;
            21..50 : begin
                        sys4.body_cover_type:=8;
                        sys4.body_type:=2;
                        if sys4.limbs_number>2 then sys4.limbs_number:=2;
                     end;
            51..60 : begin
                        sys4.body_cover_type:=11;
                        sys4.body_type:=6;
                     end;
            61..65 : begin
                        sys4.body_cover_type:=1;
                        sys4.body_type:=1;
                        sys4.limbs_number:=0;
                     end
            else     begin
                        sys4.body_cover_type:=de(10,1);
                        if sys4.body_cover_type=8 then sys4.body_cover_type:=9;
                        sys4.body_type:=1;
                     end;
        end;
      end;
   6: begin
        case de(100,1) of
            1..6   : sys4.limbs_number:=0;
            7..70  : sys4.limbs_number:=2;
	    71..90 : sys4.limbs_number:=3;
            91..98 : sys4.limbs_number:=4;
	    99..100: sys4.limbs_number:=(de(6,1)+4);
        end;
        case de(6,1) of
            1  : begin
                   sys4.body_type:=9;
                   sys4.body_cover_type:=9;
                 end;
            2,3: begin
                   sys4.body_cover_type:=10;
                   sys4.body_type:=7;
                 end;
            4  : begin
                   sys4.body_cover_type:=9;
                   sys4.body_type:=1;
                 end;
            5  : begin
                   sys4.body_cover_type:=9;
                   sys4.body_type:=5;
                   sys4.limbs_number:=0;
                 end;
            6  : begin
                   sys4.body_cover_type:=9;
                   sys4.body_type:=8;
                   sys4.limbs_number:=0;
                 end;
        end;
      end;
end;
end;


procedure body_proc(var sys4:alien_record;atmos_comp:byte;
  warm_blood:boolean;var wings,fins:boolean;temperature:smallint;
  var color_nbr:byte);
var a,tmp,aux:byte;
begin
  main_body(sys4,warm_blood);
  if ((de(3,1)=1) and (sys4.body_type=1) and (atmos_comp<>2)) or
     ((de(10,1)=1) and (sys4.body_type=1) and (atmos_comp=2))
     then
     begin
       case de(10,1) of
           1..2 : begin
                    sys4.body_type:=2;
                    sys4.body_cover_type:=8;
                  end;
           3..6 : sys4.body_type:=3;
           7..10: sys4.body_type:=4;
       end;
     end;
  case atmos_comp of
    11    : if de(3,1)>1 then
                     begin
                      sys4.body_cover_type:=8;
                      sys4.body_type:=2;
                     end;
    18,19 : if de(3,1)>1 then
                     begin
                      sys4.body_cover_type:=8;
                      sys4.body_type:=3;
                     end;
  end;

  {---body color procedure---}
  a:=de(3,1);  { a=3 -> different colors }
  if a=3 then color_nbr:=de(5,1)+1
     else color_nbr:=1;
  for a:=1 to color_nbr do
     begin
        aux:=de(11,1);
        if aux=11 then aux:=10+de(6,1);
        tmp:=1+trunc((aux-1)/8);
        set_bit(sys4.color_genre[tmp],aux-(tmp-1)*8);
     end;
  if color_nbr=1 then set_bit(sys4.body_char[2],3);
  if color_nbr>1 then
  case de(10,1) of
     1..7: set_bit(sys4.body_char[2],3); { One tone}
     8..9: begin
              set_bit(sys4.body_char[2],4);
              aux:=de(10,1);
              if aux<6 then set_bit(sys4.body_char[2],6)
                 else if aux<8 then set_bit(sys4.body_char[2],7)
                    else set_bit(sys4.body_char[2],8);
           end;
     10  : begin
               if color_nbr>2 then set_bit(sys4.body_char[2],5)
                  else set_bit(sys4.body_char[1],8);
               if de(10,1)<5 then set_bit(sys4.body_char[2],7)
                   else set_bit(sys4.body_char[2],8);
           end;
  end;

  {---limbs procedure---}
  if (sys4.limbs_number=2) and (sys4.environment_type=5) then sys4.limbs_number:=3;
  wings:=false;
  fins:=false;
  if de(10,1)>1 then sys4.limbs_genre[1]:=5
     else sys4.limbs_genre[1]:=4;
  if sys4.environment_type=4 then
     begin
        sys4.limbs_genre[2]:=2;
        fins:=true;
     end
     else sys4.limbs_genre[2]:=3;
  for a:=3 to sys4.limbs_number do
    begin
      case de(100,1) of
	   1..10: if sys4.environment_type=5 then begin
					       sys4.limbs_genre[a]:=1;
					       wings:=true;
					     end
		     else if (sys4.environment_type=3) or (sys4.environment_type=4)
		       then begin
			       sys4.limbs_genre[a]:=2;
			       fins:=true;
			    end
			    else sys4.limbs_genre[a]:=3;
	   11..50: if sys4.environment_type=4 then sys4.limbs_genre[a]:=2
		       else sys4.limbs_genre[a]:=3;
	   51..65: if sys4.environment_type=4 then sys4.limbs_genre[a]:=4
                       else sys4.limbs_genre[a]:=3;
	   66..70: sys4.limbs_genre[a]:=4;
	   71..95: sys4.limbs_genre[a]:=5;
           96..100: sys4.limbs_genre[a]:=6;
       end;
     end;
  if not (wings) and (sys4.environment_type=5) then
     begin
        sys4.limbs_genre[3]:=1;
        wings:=true;
     end;
end;


procedure body_proc2(var sys4:alien_record;color_nbr:byte;blind:boolean);
var a,tmp,aux,aux2:byte;
begin
  {--- Hair ---}
  sys4.hair_char:=8;
  case sys4.app_genre of
   1: if de(12,1)=1 then sys4.hair_char:=1;
   2,3,8,9,15,16,17,18,19,20,21: sys4.hair_char:=1;
   13: if de(10,1)>1 then sys4.hair_char:=1;
  end;
  if sys4.body_cover_type>4 then sys4.hair_char:=1;

  if (sys4.body_cover_type=4) and (de(10,1)>1) then
     sys4.hair_char:=7;
  if sys4.hair_char=8 then
     case de(50,1) of
       1,2 : sys4.hair_char:=2;
       3..6: sys4.hair_char:=5;
       7..9: if sys4.app_genre=13 then
              sys4.hair_char:=4
     end;
  if (sys4.hair_char=1) and (de(10,1)=1) then sys4.hair_char:=3;
  if sys4.hair_char<>1 then
     begin
        aux:=color_nbr+de(3,1)-2;
        if aux<0 then aux:=1;
        for a:=1 to aux do
          begin
             aux2:=de(11,1);
             if aux2=11 then aux2:=10+de(6,1);
             tmp:=1+trunc((aux2-1)/8);
             set_bit(sys4.hair_color[tmp],aux2-(tmp-1)*8);
          end;
     end;
  if sys4.body_cover_type=3 then
     begin
       if de(5,1)>1 then sys4.hair_color:=sys4.color_genre;
       if de(10,1)>1 then sys4.hair_char:=6;
     end;
  if (sys4.hair_color[1]=0) and (sys4.hair_color[2]=0) and
      (sys4.hair_char<>1) then sys4.hair_color:=sys4.color_genre;

  {---Eyes characteristics---}
  if blind then
      set_bit(sys4.eye_char[1],1);
  if is_set(sys4.eye_char[1],1) then
        unset_bit(sys4.table_abil[2],5);
  if not is_set(sys4.eye_char[1],1) then
      begin
        case sys4.app_genre of
          2,21: set_bit(sys4.eye_char[2],6);
          3,16: if de(10,1)>3 then set_bit(sys4.eye_char[2],4)
                else set_bit(sys4.eye_char[2],5);
          17,18: set_bit(sys4.eye_char[2],5);
          else begin
                  aux:=de(10,1);
                  if aux<9 then set_bit(sys4.eye_char[2],3)
                   else if aux=9 then set_bit(sys4.eye_char[2],4)
                     else if de(10,1)>1 then set_bit(sys4.eye_char[2],5)
                        else set_bit(sys4.eye_char[2],6);
                end;
        end;
        case de(100,1) of  {number of eyes}
          1..3: set_bit(sys4.eye_char[1],2);  {1}
          4,5 : set_bit(sys4.eye_char[1],3);  {3}
          6..8: set_bit(sys4.eye_char[1],4);   {4}
          9   : set_bit(sys4.eye_char[1],5);  {multiple}
        end;
        case de(20,1) of  {eyes appearance}
          1:set_bit(sys4.eye_char[1],6);
          2:set_bit(sys4.eye_char[1],7);
          3:set_bit(sys4.eye_char[1],8);
          4:set_bit(sys4.eye_char[2],1);
          5:if (sys4.app_genre=15) or (sys4.app_genre=20) then
            set_bit(sys4.eye_char[2],2);
        end;
        aux:=color_nbr+de(3,1)-2;  {color}
        if aux=0 then aux:=1;
        for a:=1 to aux do
               begin
                   aux2:=de(11,1);
                   if aux2=11 then aux2:=10+de(6,1);
                   tmp:=1+trunc((aux2-1)/8);
                   set_bit(sys4.eye_color[tmp],aux2-(tmp-1)*8);
               end;
       end;
end;

procedure main_alien (temperature:smallint;gravity:single;atmos_comp,world_type:
    byte;new_file:boolean;outstr:string;mother_id:longint;al_disable:boolean;
    max_spatial:byte);
var a,color_nbr:byte;
    datafile:string;
    warm_blood,blind,wings,fins:boolean;
    sys4: alien_record;
    afile : file of alien_record;
begin
  datafile:=concat(outstr,'.aln');
  assign(afile,datafile);
  {$I-}
  if new_file then rewrite(afile)
     else reset(afile);
  close(afile);
  {$I+}

  if IoResult<>0 then rewrite(afile)  {creation of a new file if this file
                                       doesn't exist}
                 else reset(afile);
  seek(afile,filesize(afile));
  for a:=1 to 2 do
    begin
       sys4.color_genre[a]:=0;
       sys4.hair_color[a]:=0;
       sys4.body_char[a]:=0;
       sys4.eye_color[a]:=0;
       sys4.eye_char[a]:=0;
    end;
  sys4.hair_char:=0;
  sys4.name:='';
  for a:= 1 to 10 do sys4.table_abil[a]:=0;
  case de(100,1) of
    1..75 :sys4.environment_type:=1;
    76,77 :sys4.environment_type:=2;
    78..91:sys4.environment_type:=3;
    92..94:sys4.environment_type:=4;
    95..100:sys4.environment_type:=5;
  end;
  if (gravity>2) and (sys4.environment_type=5) then sys4.environment_type:=1;
  case world_type of
    8 : if sys4.environment_type=4 then sys4.environment_type:=3;
    12: if de(10,1)>3 then sys4.environment_type:=4
           else sys4.environment_type:=3;
  end;
  if (de(100,1)>20) or (temperature<5) then warm_blood:=true else warm_blood:=false;
  body_proc(sys4,atmos_comp,warm_blood,wings,fins,temperature,color_nbr);
  case de(100,1)  of
	 1..15 :sys4.diet_genre:=1;
	 16..75:sys4.diet_genre:=2;
	 76..96:sys4.diet_genre:=3;
         97..100:sys4.diet_genre:=4;
  end;
  case de(100,1) of
	 1..5  : sys4.repro_genre:=1;
	 6..10 : sys4.repro_genre:=2;
	 11..95: sys4.repro_genre:=3;
         96..99: sys4.repro_genre:=5;
	 100   : sys4.repro_genre:=4;
  end;
  case de(100,1) of
	 1     : sys4.repro_meth_genre:=1;
	 2..10 : sys4.repro_meth_genre:=2;
	 11..70: if warm_blood then sys4.repro_meth_genre:=3
		    else sys4.repro_meth_genre:=2;
	 71..98: sys4.repro_meth_genre:=3;
	 99..100: sys4.repro_meth_genre:=4;
  end;
  size_calcul(gravity,sys4);
  charac(sys4,atmos_comp,max_spatial);
  blind:=true;
  ability_calcul(gravity,temperature,fins,wings,warm_blood,atmos_comp,sys4,
    al_disable,blind);
  body_proc2(sys4,color_nbr,blind);
  gov_calcul(sys4);
  religion_determination(sys4);
  if sys4.app_genre=18 then sys4.attrib[8]:=min_range(sys4.attrib[8],6);
  sys4.pln_id:=mother_id;
  write(afile,sys4);
  close(afile);
end;

end.




