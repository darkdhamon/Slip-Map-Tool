{Main procedure unit for StarDll v1.6, 04-2001

   v1.57b Quasar class, new planet unusual charac(Proto-organisms, Primitive life)
   v1.6   New ini file, new eccentricty function

}


UNIT PROCUNIT;

INTERFACE

USES recunit,Forms;


const day_table : ARRAY [1..6] of single =
            (0.2,0.4,0.5,0.6,0.8,1);
      day_max : ARRAY [1..6] of single =
            (5,4,2.5,1.5,0.8,0.1);
      night_table : ARRAY [1..6] of single =
            (0.2,0.5,1,3,8,20);
      night_max : ARRAY [1..6] of single =
            (0.05,0.1,0.15,0.3,0.5,0.8);



FUNCTION  de(face,nombre:word):word;export;
FUNCTION  power(variable,exponent:single):single;export;
FUNCTION  escape_velocity_calcul(mass:single;diameter:longint) :single;export;
PROCEDURE world_calcul(var atmos,res:byte;inz,outz,lifez,orb:smallint;
         diametre:longint;icy,gaia:boolean);export;
FUNCTION satellites_calcul(world:byte):byte;export;
FUNCTION moon_orbit(diametre:longint):smallint;export;
PROCEDURE unusual_calcul(atmos_type,world_type,satellites,water_type,
  hydrography,axial_tilt,density:byte;var atmos:byte;mine:table_num;rotation_period:longint;
  misc_charac:byte;twin,roche:boolean;eccentricity:word;temperature,
  day_temp:smallint;gravity,mass:single;var unusual:table_bool;mine_radiation,
  cloud_fraction:byte);export;
FUNCTION Molecule_limit (escape_velocity: single;r_ecosphere,orbit:smallint) : single;export;
PROCEDURE atmos_calcul(var atmos:byte;innerzone,R_ecosphere,outerzone,
      orbital_radius:smallint;mass,gravity:single;var pressure,
      volatile_gas_inventory:single;diameter:longint;icy:boolean;
      escape_velocity:single;var molecular_weight:byte);export;
FUNCTION compos_atmos(atmos_typ,world:byte;gravity,pressure,escape_velocity:single;
         R_ecosphere,orbit_radius:smallint;molecular_weight:byte):byte;export;
FUNCTION oxygene_calcul(ice,hydro,atmos_comp:byte;gravity:single):single;export;
FUNCTION temp_calcul(lux:single;orbit:smallint;atmos,world:byte;
   albedo:byte):smallint;export;
PROCEDURE temp_var(var day,night:longint;orbit:smallint;atmos:byte;rot:longint;
          temp:smallint;sys1:star_record;current_star:byte);export;
FUNCTION orbit_calcul(orbit:smallint;mass:single):longint;export;
PROCEDURE rotation_calcul(orbit:smallint;mass,gravity:single;diametre:longint;
       mass_stellar,eccentricity:single;satellites,water_type:byte;period:longint;world:byte;
       var misc_charac:byte;var res:longint;density,age:byte);export;
FUNCTION orbital_inclination_calcul(orbit_dist:smallint):byte;export;
FUNCTION eccentricity_calcul:word;export;
FUNCTION axial_tilt_calcul:byte;export;
PROCEDURE diametre_calcul(var misc_charac:byte;orbit,innerz,outerz:smallint;
	 var diametre:longint;var density:byte;gaia:boolean);export;
PROCEDURE gravity_calcul(density:byte;diametre:longint;
       var mass:single;var gravity:single;var escape_velocity:single);export;
FUNCTION hydrosphere_fraction (volatile_gas_inventory:single;planetary_diameter: longint;
         temperature:smallint)   : byte;export;
FUNCTION Ice_fraction (hydrosphere:byte; surface_temp : smallint) : byte;export;
FUNCTION Cloud_fraction (surface_temp:smallint;
   smallest_molecular_weight_retained:single;diameter:longint;
   hydrosphere,world : byte) : byte;export;
FUNCTION Planet_albedo (water_fraction, cloud_fraction, ice_fraction:byte;
                           surface_pressure : single) : byte;export;
PROCEDURE Determine_albedo_and_surface_temp(inz,ouz,orb:smallint;var world,
    wat_type:byte;var hydro:table_num;volatile_gas_inventory,lux,gravity,
    pressure:single;diameter:longint;atmos:byte; var surface_temp:smallint;
    escape_velocity:single);export;
PROCEDURE mine_calcul (diametre:longint;density:byte;var mine:table_num);export;
PROCEDURE read_ini_files;export;
PROCEDURE star_creation(var luminosity,mass:table_2;var age:byte;
    var spe_class,type_spec:table_1;var star_nbr:byte;var distance:table_0;
    gaia:boolean);export;
PROCEDURE orbit_creation(var astral_orbit:table_5;var planet_nbr:byte;
	      stellar_type:byte;mass,companion_dist,lux:single;
              current_star,star_nbr:byte;gaia:boolean);export;
PROCEDURE stardll_ini;export;
FUNCTION magnetic_calcul (resonance:boolean;mass:single;density,world_type:byte;
    rotation_period:longint): single;export



IMPLEMENTATION


Uses IniFiles,SysUtils;

type table_ini25=array[1..10] of word;

var  star_prob: table_ini25;

{ ***************************************
  de is a dice roll function
  de(6,4) is like throwing 4d6
  ***************************************}

function de(face,nombre:word):word;
   var res,i:word;
   begin
     res:=0;
     for i:=1 to nombre do
	res:=round(random(face))+res+1;
     de:=res;
   end;

function power (variable, exponent : single) : single;
  begin
         if (variable <= 0.0)
            then power:= 0.0
            else power:= exp(exponent * ln(variable));
  end;

FUNCTION  escape_velocity_calcul(mass:single;diameter:longint) :single;
begin
  if diameter>0 then escape_velocity_calcul:=sqrt(1.59E16*mass/diameter)
     else escape_velocity_calcul:=0;
end;


{ ------------ returns the world type (res) ---------------}

procedure world_calcul(var atmos,res:byte;inz,outz,lifez,orb:smallint;
         diametre:longint;icy,gaia:boolean);
    begin
      case atmos of
          1  : if icy then res:=3 else
                  if (orb<=inz) then res:=4
                     else if (orb<=lifez) then res:=21
                          else res:=5;
          2,3: if icy then res:=5 else
                  if orb<=outz then res:=10
                     else res:=13;
          4,5: if icy then res:=5 else res:=13;
          6  : if not icy then res:=2 else res:=1;
      end;
      if diametre<1000 then res:=7;
      if diametre=0 then res:=19;
  end;


{ ---- returns the number of satellites ---- }

function satellites_calcul(world:byte):byte;
       var res:smallint;
    begin
	 res:=de(6,1)-3;
	 if (res<0) then res:=0;
	 if world=3 then res:=de(6,2);
         if (world=20) and (de(6,1)>1) then res:=0;
         if world=7 then res:=0;
	 satellites_calcul:=res;
    end;


{ ---- returns the orbital radius of satellites given -----
        in planetary diameters of the parent planet
  ---------------------------------------------------------}

function moon_orbit(diametre:longint):smallint;   {diametre: sat diameter}
   var aux,factor:smallint;
   begin
      aux:=de(10,1);
      case aux of
	 1..3 :factor:=1;
	 4..6 :factor:=2;
	 else factor:=3;
      end;
      if diametre=0 then factor:=1;
      case factor of
	 1 :if diametre=0 then moon_orbit:=de(3,1)
		else moon_orbit:=de(7,1)+3;
         2 :moon_orbit:=de(10,1)*5+10;
	 else moon_orbit:=de(10,1)*10+60;
      end;
  end;

function magnetic_calcul (resonance:boolean;mass:single;density,world_type:byte;
    rotation_period:longint): single;
begin
    if not resonance then magnetic_calcul:=sqr(mass*density*5.5/100)*
                                    (24/rotation_period)
       else magnetic_calcul:=sqr(mass*density*5.5/100);
    if (world_type=3) or (world_type=20) then
       magnetic_calcul:=(mass*density*5.5/10)*(2.5/rotation_period)/10;
end;



{ ------- returns the unusual characteristics ------
  life_chance>47 proto-organisms
  life_chance>50 primitive lifeforms
  life_chance>55 semi-intelligent lifeforms
  life_chance>74 intelligent lifeforms }

procedure unusual_calcul(atmos_type,world_type,satellites,water_type,
  hydrography,axial_tilt,density:byte;var atmos:byte;mine:table_num;rotation_period:longint;
  misc_charac:byte;twin,roche:boolean;eccentricity:word;temperature,
  day_temp:smallint;gravity,mass:single;var unusual:table_bool;mine_radiation,
  cloud_fraction:byte);
var life_chance,aux:smallint;
    water_vapor,magnetic:single;
    icy_core,resonance:boolean;
begin
    icy_core:=is_set(misc_charac,1);
    resonance:=is_set(misc_charac,2);
    for aux:=1 to 5 do unusual[aux]:=0;
    life_chance:=0;
    if  (not icy_core) and (satellites>0) and (de(75,1)=1) then
         begin
            set_bit(unusual[1],1);
            if (atmos_type<5) and (de(10,2)<7) then
               begin
                 set_bit(unusual[1],2);
                 case atmos of
                   3 : atmos:=11;
                   5 : atmos:=18;
                   9 : atmos:=14;
                 end;
               end;
            life_chance:=-5;
            if de(10,2)<5 then set_bit(unusual[2],4);
         end;
    if  (atmos_type<5) and (de(75,1)=1) then
         begin
	    set_bit(unusual[1],2);
	    life_chance:=life_chance-5;
         end;
    if de(75,1)=1 then
         begin
	    set_bit(unusual[1],3);
	    life_chance:=life_chance-5;
	 end;
    if de(50,1)=1 then set_bit(unusual[2],1);
    if ((atmos_type>4) and (de(10,1)=1)) or (de(100,1)<(mine_radiation/3)) or
       (de(200,1)=1) then
       	 begin
            set_bit(unusual[1],4);
            life_chance:=life_chance-15;
	 end;
    if (atmos_type<5) and ((de(30,1)=1) or ((de(6,1)=1) and (axial_tilt<5)))
        then  begin
                set_bit(unusual[1],5);
                life_chance:=life_chance-5;
              end;
    if (atmos_type<5) and (de(50,1)=1) then
         begin
	    set_bit(unusual[1],6);
	    life_chance:=life_chance-5;
	 end;
    if de(200,1)=1 then
         begin
	    set_bit(unusual[1],7);
	    life_chance:=life_chance-20;
	 end;
    if (not icy_core) and (world_type <>12) and (de(50,1)=1) then
        set_bit(unusual[1],8);
    if (de(50,1)=1) and (atmos_type<5) then
        begin
          set_bit(unusual[2],2);
          life_chance:=life_chance-10;
        end;
    if cloud_fraction>90 then set_bit(unusual[2],6);
    if (hydrography>50) and (water_type=5) and (satellites>0) and
           (de(6,1)=1) then
		  begin
		    set_bit(unusual[2],8);
		    life_chance:=life_chance-5;
		  end;
    if (axial_tilt>50) then
	          begin
		     life_chance:=life_chance-15;
		     set_bit(unusual[3],2);
		     if (de(6,1)<4) then set_bit(unusual[2],5);
	           end;
    if (axial_tilt<5) then
	          begin
		      set_bit(unusual[2],7);
		      life_chance:=life_chance+10;
		  end;
    if resonance then set_bit(unusual[3],1);
    magnetic:=magnetic_calcul(resonance,mass,density,world_type,rotation_period);
    if magnetic>5 then
                  begin
		      life_chance:=life_chance-10;
		      set_bit(unusual[2],5);
		  end;
    if twin then set_bit(unusual[4],1);
    if roche then set_bit(unusual[4],2);
    if (atmos_type<3) and (de(10,1)=1) then set_bit(unusual[4],3);
    if (eccentricity>250) then
		  begin
	  	      life_chance:=life_chance-5;
		      if not icy_core then
                           begin
                             set_bit(unusual[1],1);
		             if (de(10,2)<7) and (atmos_type<4) then
                                 set_bit(unusual[1],2);
                           end;
		      set_bit(unusual[2],3);
		  end;
     if (water_type=5) and (temperature>5) then
       begin
        if temperature>35 then water_vapor:=31000
           else water_vapor:=exp(0.698*(temperature-15))*hydrography/(0.007*gravity);
        if (water_vapor>30000) and (temperature>25) then set_bit(unusual[3],5);
        if water_vapor<0.2 then set_bit(unusual[3],6);
       end;
     case world_type of
		   10,11,12 : life_chance:=life_chance+25;
		   8,9,23   : life_chance:=life_chance+10;
		   13       : life_chance:=life_chance-40;
		   14       : life_chance:=life_chance-35;
		   5        : life_chance:=life_chance-45;
                   22       : life_chance:=life_chance-60;
		   else  life_chance:=life_chance-200;
     end;
     if atmos<>2 then life_chance:=life_chance-10;
     aux:=de(100,1)+life_chance;
     case aux of
       48..50 : set_bit(unusual[5],2);
       51..55 : set_bit(unusual[5],3);
       56..74 : set_bit(unusual[3],4);
       75..200: set_bit(unusual[3],3);
     end;
     if (atmos>3) and (atmos<>8) and ((temperature+day_temp)>50)
        then set_bit(unusual[3],7);
     if (atmos=6) or (atmos=20) or ((atmos=4) and ((temperature+day_temp)>100))
        then set_bit(unusual[3],8);
     if is_set(unusual[1],4) then set_bit(unusual[3],8);
     if is_set(unusual[3],8) then unset_bit(unusual[3],7);
     if (aux>0) and (de(200,1)=1) then set_bit(unusual[4],4);  {Alien artifacts}
     if (aux>0) and (not is_set(unusual[3],3)) and (de(100,1)=1) then
        set_bit(unusual[4],6); {Remains of dead civ.}
     if is_set(unusual[4],6) and (de(20,1)=1) then set_bit(unusual[4],8); {Wonders}
end;

{ --------------------------------------------------------------------------
   This function returns the smallest molecular weight retained by the
  planet.
  --------------------------------------------------------------------------}

function Molecule_limit (escape_velocity: single;r_ecosphere,
         orbit:smallint) : single;
  var err_mess:smallint;
  begin
    if escape_velocity=0 then
       err_mess:=Application.MessageBox('** Error 3: escape_velocity=0 ','Error',0);
    if orbit=0 then
       err_mess:=Application.MessageBox('** Error 4: orbit radius=0','Error',0);
    if orbit<>0 then molecule_limit:=7.94E12*sqrt(r_ecosphere/orbit)/sqr(escape_velocity)
       else molecule_limit:=121;
  end;


{ -------- returns the atmosphere type ------------}

procedure atmos_calcul(var atmos:byte;innerzone,R_ecosphere,outerzone,
      orbital_radius:smallint;mass,gravity:single;var pressure,
      volatile_gas_inventory:single;diameter:longint;icy:boolean;
      escape_velocity:single;var molecular_weight:byte);
  var exospheric_temp,RMS_vel,velocity_ratio:single;
      proportion_const,aux:single;
      err_mess:smallint;
  const Molar_gas_const = 8314.41;
        Earth_exosphere_temp = 1273;
        molecular_nitrogen=28;
  begin
        if gravity>0 then aux:=Molecule_limit(escape_velocity,
           R_ecosphere,orbital_radius)
           else aux:=121;
        if aux>121 then aux:=121;
        if aux=0 then aux:=1;
        molecular_weight:=trunc(aux);
        if mass>=10 then pressure:=900;
        if molecular_weight>120 then
           begin
             pressure:=0;
             volatile_gas_inventory:=0;
           end;
        if (mass<10) and (molecular_weight<=120) then
        begin
          if orbital_radius<=4*R_ecosphere then proportion_const:=100
             else if orbital_radius<=15*R_ecosphere then proportion_const:=75
                  else proportion_const:=0.25;
          exospheric_temp:= Earth_exosphere_temp*sqrt(R_ecosphere/orbital_radius);
          RMS_vel:= Sqrt((3*Molar_Gas_const * exospheric_temp)/molecular_nitrogen)
                  * 100;
          if RMS_vel=0 then
             err_mess:=Application.MessageBox('Error 5: RMS_vel=0','Error',0);
          velocity_ratio:=escape_velocity/ RMS_vel;
          if velocity_ratio>5 then
                  volatile_gas_inventory:=proportion_const*mass
                     else volatile_gas_inventory:=0;
          if orbital_radius>=innerzone then
                  volatile_gas_inventory:=volatile_gas_inventory/100;
          pressure:=volatile_gas_inventory*gravity*sqr(diameter/12750);
          if (orbital_radius<innerzone) and (molecular_weight<49) then
            pressure:=pressure*1.35;
          if icy and (molecular_weight<=28) then pressure:=pressure*10;
        end;
     if pressure<0.001 then begin
                               atmos:=6;
                               molecular_weight:=121;
                            end
        else if pressure<0.2 then atmos:=5
             else if pressure<0.65 then atmos:=4
                  else if pressure<1.5 then atmos:=3
                       else if pressure<5 then atmos:=2
                            else atmos:=1;
  end;


{ ----- returns the atmospher's active gas(es) --------- }

function compos_atmos(atmos_typ,world:byte;gravity,pressure,escape_velocity:single;
         R_ecosphere,orbit_radius:smallint;molecular_weight:byte):byte;
   const molecular_table : ARRAY [1..21] of byte=
      (200,28,44,28,78,2,17,16,28,50,44,28,44,34,28,16,44,
       78,28,2,16);
   var res,x:byte;
	exotic:boolean;
        err_mess:smallint;
   begin
     if (gravity=0) or (pressure<0.001) then res:=1
     else
        begin
          Exotic:=false;
          case world of
               2       : res:=3;
               4,21    : begin
                           res:=3;
                           if molecular_weight>molecular_table[3] then
                              err_mess:=Application.MessageBox('Error 1 (Hot House)',
                                        'Error',0);
                         end;
               3,20    : begin
                            if de(10,1)>3 then res:=6 else res:=20;
                            if molecular_weight>molecular_table[6] then
                               case de(10,1) of
                                   1..4 : res:=7;
                                   5..8 : res:=8;
                                   9..10: res:=16;
                               end;
                            if molecular_weight>molecular_table[16] then
                              err_mess:=Application.MessageBox('Error 1 (Gas Giant)',
                                        'Error',0);
                         end;
               1,5     : begin
                            if molecular_weight<=17 then
                               begin
                                 if de(10,1)>5 then res:=7 else res:=8;
                               end
                            else if molecular_weight<=28 then res:=4
                                 else res:=3;
                            if molecular_weight>44 then
                               err_mess:=Application.MessageBox('Error 1 (Icy)',
                                        'Error',0);
                         end;
               8..12,14,23,24: begin
		            x:=de(10,1);
		            if (((x>2) and (atmos_typ=3)) or ((x>3) and (atmos_typ=2)))
		               and (molecular_weight<=molecular_table[2]) then res:=2
			       else exotic:=true;
                         end;
               13      : if (de(10,1)<9) and (molecular_weight<=molecular_table[3])
                            then res:=3
                            else exotic:=true;
               22      : begin
                           res:=21;
                           if molecular_weight>molecular_table[21] then res:=4
                              else if molecular_weight>molecular_table[4] then
                              err_mess:=Application.MessageBox('Error 1 (Post Garden)',
                                        'Error',0);
                         end;
          end;
          if exotic then
	     begin
	          x:=de(20,1);
                  case x of
                       1..5  : res:=4;
	               6,7   : res:=3;
                       8     : res:=13;
                       9     : res:=15;
                       10    : res:=17;
	               11,12 : res:=5;
	               13,14 : res:=9;
	               15,16 : res:=10;
                       17    : res:=12;
                       18,19 : res:=16;
                       20    : res:=19;
                  end;
                  if molecular_weight>molecular_table[res] then
                     res:=3;
	     end;
        end;
     compos_atmos:=res;
   end;


{ ----- returns the oxygen pressure based on water surface coverage ----- }

function oxygene_calcul(ice,hydro,atmos_comp:byte;gravity:single):single;
   const oxygene_table : ARRAY [1..11] of byte =
      (5,10,12,14,16,18,19,20,22,24,26);
   var res:single;
	 x,y,z:byte;
   begin
     res:=0;
     x:=trunc(hydro/10);
     y:=trunc(ice/10);
     if y>x then z:=y else z:=x;
     if atmos_comp=2 then res:=oxygene_table[z+1]*gravity;
     if y>x then res:=res/3;
     oxygene_calcul:=res/100;
   end;


{ ----- returns the average surface temperature ------
    lux=effective luminosity of the star(s)
    green_effect=greenhouse effect
    energy_abs=energy absorption of the world
  ---------------------------------------------------- }

function temp_calcul(lux:single;orbit:smallint;atmos,world:byte;
   albedo:byte):smallint;
   var res,green_effect,energy_abs:single;
   begin
          energy_abs:=power((1-(albedo/100))/0.7, 0.25);
          case atmos of
            1: green_effect:=1.25;
            2: green_effect:=1.20;
            3: green_effect:=1.15;
            4: green_effect:=1.1;
            5: green_effect:=1.05;
            6: green_effect:=1;
          end;
          if world=4 then green_effect:=green_effect*2;
	  res:=sqrt(sqrt(lux)/(orbit/10))*263*energy_abs*green_effect-273;
	  temp_calcul:=trunc(res);
   end;


{ ---- returns the day and night temperature variation ----- }

procedure temp_var(var day,night:longint;orbit:smallint;atmos:byte;rot:longint;
          temp:smallint;sys1:star_record;current_star:byte);
    var rot_lux,lux:single;
        max_day,max_night:longint;
    begin
        lux:=sys1.luminosity[current_star];
        case sys1.star_nbr of
            2: if current_star=1 then lux:=lux+sys1.luminosity[2]/(sqrt(sys1.companion_dist[1]/10))
	               else lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[1]/10));
            3: if current_star=1 then lux:=lux+sys1.luminosity[2]/(sqrt(sys1.companion_dist[1]/10))
                       +sys1.luminosity[3]/(sqrt(sys1.companion_dist[2]/10))
                 else if current_star=2 then lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[1]/10))+
                       sys1.luminosity[3]/(sqrt((sys1.companion_dist[1]+sys1.companion_dist[2])/10))
                 else lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[2]/10))+
                       sys1.luminosity[2]/(sqrt((sys1.companion_dist[1]+sys1.companion_dist[2])/10));
        end;
	rot_lux:=lux/sqrt(orbit/10);
	rot:=abs(rot);
	temp:=temp+273;
	day:=trunc(day_table[atmos]*rot_lux*rot/2);
	night:=trunc(night_table[atmos]*rot/2);
	max_day:=trunc(temp*day_max[atmos]*rot_lux);
	max_night:=trunc(temp*night_max[atmos]);
	if (day>max_day) then day:=max_day;
        if (day+temp)>2000 then day:=2000-temp;
	if (night>max_night) then night:=max_night;
    end;


{ ------ returns the orbit period ------ }

function orbit_calcul(orbit:smallint;mass:single):longint;
      var res:single;
   begin
      res:=sqrt(power(orbit/10,3)/mass);
      res:=res*365;
      orbit_calcul:=round(res);
   end;



{ ----- returns the rotation period ------- }

procedure rotation_calcul(orbit:smallint;mass,gravity:single;diametre:longint;
       mass_stellar,eccentricity:single;satellites,water_type:byte;period:longint;world:byte;
       var misc_charac:byte;var res:longint;density,age:byte);

  var K,aux,angular_velocity:single;
  begin
      unset_bit(misc_charac,2);
      K:=0.19;
      if (world=3) or (world=20) then K:=0.49;
      if (water_type=5) and (satellites>0) then K:=0.33;
      angular_velocity:=sqrt((mass*7.05E8)/(K*sqr(diametre*5E4)));
      aux:=6.28/(3600*angular_velocity);
      res:=trunc(aux);
      aux:=(exp(0.27*ln(10/orbit)))/mass_stellar;
      res:=trunc(res*aux);
      if orbit<(5*mass_stellar) then res:=0;
      aux:=sqrt(2*Pi/(0.19*gravity));
      if (res>0) and (res<aux) then res:=trunc(aux);
      if (res>(period*24)) then
           begin
	     res:=period*24;
	     if (eccentricity>100) then res:=round((1-eccentricity/1000)
                 *res/(1+eccentricity/1000));
	     set_bit(misc_charac,2);
	   end;
      if res=0 then
           begin
             res:=period*24;
             set_bit(misc_charac,2);
           end;
   end;


function orbital_inclination_calcul(orbit_dist:smallint):byte;
  var aux:single;
  begin
    aux:=(1-power((de(50,2)-1)/100,0.077));
    aux:=aux*28.65*power(orbit_dist/10,0.2);
    if aux>180 then aux:=180;
    orbital_inclination_calcul:=round(aux);
  end;



{ ---- returns the world eccentricity ---- }

function eccentricity_calcul:word;
   var x:byte;
	  res:word;
   begin
      x:=de(6,2);
      case x of
       2..6: res:=0;
       7..8: res:=de(5,1);
       9   : res:=de(5,1)+5;
       10  : res:=de(5,1)+10;
       11  : res:=de(10,1)+15;
      end;
      if x=12 then
	   begin
	     x:=de(6,1);
             case x of
	       1 : res:=de(5,1)+20;
	       2 : res:=de(25,1)+25;
	       3 : res:=de(50,1)+50;
	       4 : res:=de(100,1)+100;
	       5 : res:=de(50,1)+200;
	       6 : res:=200+de(600,1);
             end;
	   end;
      eccentricity_calcul:=res;
   end;


{ ---- returns the axial tilt ---- }

function axial_tilt_calcul:byte;
   var aux:byte;
   begin
       aux:=de(6,2);
       if (aux<12) then axial_tilt_calcul:=de(6,2)-2+trunc(aux/2-1)*10
	   else  axial_tilt_calcul:=38+de(4,1)*10+de(6,2);
   end;


{ ---- returns the world diameter and density ---- }

procedure diametre_calcul(var misc_charac:byte;orbit,innerz,outerz:smallint;
	 var diametre:longint;var density:byte;gaia:boolean);
   var res,dice:byte;
   begin
      unset_bit(misc_charac,1);
      if (orbit>outerz) then
          begin
            dice:=de(6,1);
            if (orbit<2*outerz) and (dice>4) then set_bit(misc_charac,1)
                else if dice>1 then set_bit(misc_charac,1);
          end;
      if not is_set(misc_charac,1) then
	  begin
	    if gaia and (orbit>outerz) and (orbit<innerz) then
               diametre:=de(10,2)
               else diametre:=de(6,de(4,1));
	    density:=de(6,1)+6;
	  end
      else
	  begin
	    res:=de(10,1);
            case res of
                 1..2: diametre:=de(6,2);
                 3..4: diametre:=de(6,6)+30;
                 5..8: diametre:=de(10,6)+50;
                 9   : diametre:=de(10,10)+50;
                 10  : diametre:=de(25,10)+50;
            end;
            if diametre>40000 then density:=de(4,1)
               else density:=de(3,1)+2;
	  end;
      diametre:=diametre*1000+de(999,1);
    end;


{ ---------- returns the world mass and gravity ---------------------}

procedure gravity_calcul(density:byte;diametre:longint;
    var mass:single;var gravity:single;var escape_velocity:single);
   begin
     gravity:=pi*density*(diametre/1000)*169/67650;
     mass:=pi*density*power(diametre/2000,3)/8139;
     escape_velocity:=escape_velocity_calcul(mass,diametre);
   end;



{--------------------------------------------------------------------------
   Given the volatile gas inventory and planetary diameter of a planet
   (in Km), this function returns the fraction of the planet covered with
   water.
 --------------------------------------------------------------------------}

function hydrosphere_fraction (volatile_gas_inventory:single;planetary_diameter: longint;
         temperature:smallint)
   : byte;
      var temp : longint;
          aux:single;
      begin
         aux:=71 * volatile_gas_inventory *Sqr(12750/planetary_diameter);
         if aux>10000 then aux:=10000;
         temp:=trunc(aux);
         if temperature>30 then temp:=trunc(temp/3.5)
            else if temperature>35 then temp:=trunc(temp/5)
                 else if temperature>40 then temp:=trunc(temp/10)
                     else if temperature>50 then temp:=trunc(temp/20)
                       else if temperature>60 then temp:=trunc(temp/100);
         if (temp >= 100)
            then hydrosphere_fraction:= 100
            else hydrosphere_fraction:= temp;
      end;


{--------------------------------------------------------------------------
   Given the surface temperature of a planet (in Celcius), this function
   returns the fraction of the planet's surface covered by ice.
 --------------------------------------------------------------------------}

function Ice_fraction (hydrosphere:byte; surface_temp : smallint) : byte;
    var temp : single;
    begin
         temp:= power(((55 - surface_temp) / 90.0),5)*100;
         if (temp > 1.5*hydrosphere)
            then temp:=1.5*hydrosphere;
         if (temp >= 100)
            then ice_fraction:= 100
            else ice_fraction:= round(temp);
     end;


{--------------------------------------------------------------------------
   Given the surface temperature of a planet (in Celcius), this function
   returns the fraction of cloud cover available.
 --------------------------------------------------------------------------}

function Cloud_fraction (surface_temp:smallint;
   smallest_molecular_weight_retained:single;diameter:longint;
   hydrosphere,world:byte) : byte;
    const q1 = 1.258E19;   { grams    }
          q2 = 0.0698;     { 1/Kelvin }
          Earth_water_mass_per_area = 3.83E15; { grams per square km }
          Earth_area_covered_per_kg_of_cloud = 1.839E-8; { Km2/kg }

    var fraction,aux,molecular_weight: single;
        temp:smallint;
    begin
        case world of
          1,5     : begin
                       molecular_weight:=51;
                       temp:=-150;
                    end;
          else      begin
                       molecular_weight:=18;
                       temp:=15;
                    end;
        end;

        if (smallest_molecular_weight_retained > molecular_weight)
            then cloud_fraction:=0
            else begin
                    aux:=Q2*(surface_temp-temp);
                    if aux<40 then
                       begin
                         fraction:=0.7*(hydrosphere/100)*exp(aux);
                         if (fraction >= 1.0)
                               then cloud_fraction:= 100
                               else cloud_fraction:= round(fraction*100);
                        end
                        else cloud_fraction:=100;
                 end;
    end;


{--------------------------------------------------------------------------}

function Planet_albedo (water_fraction, cloud_fraction, ice_fraction:byte;
                           surface_pressure : single) : byte;
    var  components,rock_fraction,cloud_adjustment, cloud_contribution,
         rock_contribution, water_contribution, ice_contribution : byte;
    begin
         rock_fraction:= 100 - water_fraction - ice_fraction;
         components:= 0;
         if (water_fraction > 0) then components:= components + 1;
         if (ice_fraction > 0) then components:= components + 1;
         if (rock_fraction > 0) then components:= components + 1;
         cloud_adjustment:=round(cloud_fraction / components);
         if (rock_fraction >= cloud_adjustment)
            then rock_fraction:= rock_fraction - cloud_adjustment
            else rock_fraction:= 0;
         if (water_fraction > cloud_adjustment)
            then water_fraction:= water_fraction - cloud_adjustment
            else water_fraction:= 0;
         if (ice_fraction > cloud_adjustment)
            then ice_fraction:= ice_fraction - cloud_adjustment
            else ice_fraction:= 0;
         cloud_contribution:=round(cloud_fraction * (45+de(25,1))/100);
         if (surface_pressure = 0)
            then rock_contribution:=round(rock_fraction * (de(10,1)+1)/100)
            else rock_contribution:=round(rock_fraction * (de(10,1)+5)/100);
         water_contribution:=round(water_fraction * (de(20,1)+1)/100);
         if (surface_pressure = 0)
            then ice_contribution:=round(ice_fraction * (10+de(80,1))/100)
            else ice_contribution:=round(ice_fraction * (60+de(20,1))/100);
         planet_albedo:= cloud_contribution + rock_contribution
                         + water_contribution + ice_contribution;
    end;


procedure Determine_albedo_and_surface_temp(inz,ouz,orb:smallint;var world,
    wat_type:byte;var hydro:table_num;volatile_gas_inventory,lux,gravity,
    pressure:single;diameter:longint;atmos:byte;var surface_temp:smallint;
    escape_velocity:single);
    var res,a,water,ice,clouds,albedo:byte;
        previous_temp:smallint;
        molecular_weight:single;
    begin
	for a:=1 to 4 do hydro[a]:=0;
        case world of
         1: res:=3;
         6: res:=2;
         2: if orb>ouz then res:=6 else res:=2;
         3,20: res:=4;
         5,14 : res:=6;
         13: if orb<ouz then res:=2 else res:=6;
         7: if inz<orb then res:=2;
         10,22 : res:=5;
         19: res:=3;
         else res:=1;
        end;
        case world of
         10,22 : albedo:=40;
         2     : albedo:=10;
         3,20  : albedo:=50;
         else    albedo:=30;
        end;
	wat_type:=res;
        surface_temp:=temp_calcul(lux,orb,atmos,world,albedo);
        if (world<>3) and (world<>20) and (world<>19) and (gravity>0) then
            molecular_weight:=Molecule_limit(escape_velocity,trunc(sqrt(lux)*10),
                    orb);
        if (world<>3) and (world<>20) and (world<>19) and (gravity>0) then
         repeat
            previous_temp:= surface_temp;
            if (diameter<100) or (gravity=0) then water:=0
            else water:= hydrosphere_fraction(volatile_gas_inventory,diameter,
                    previous_temp);
            case res of
               3: water:=50;
               6: water:=15;
            end;
            if atmos=6 then water:=0;
            if atmos=5 then water:=round(water/2);
            clouds:= Cloud_fraction(surface_temp,molecular_weight,
                     diameter, water, world);
            case res of
               2: water:=1;
               3: water:=100;
               6: water:=10;
            end;   
            ice:= Ice_fraction(water, surface_temp);
            if (water+ice)>100 then water:=100-ice;
            if res<>5 then water:=0;
            albedo:= Planet_albedo(water, clouds, ice, pressure);
            surface_temp:= temp_calcul(lux,orb,atmos,world,albedo);
         until (abs(surface_temp - previous_temp) <= 1)
         else if (gravity>0) and (world<>19) then begin
                albedo:=40+de(20,1);
                water:=0;
                clouds:=100;
                ice:=0;
              end;
        if world=19 then
             begin
               albedo:=50;
               water:=0;
               clouds:=0;
               ice:=30;
             end;
        if world=10 then
             begin
               case water of
                   0..19  : world:=8;
                   20..49 : world:=9;
                   50..70 : world:=10;
                   71..89 : if surface_temp>25 then world:=11
                            else world:=10;
                   else world:=12;
                 end;
                 if surface_temp<5 then world:=23;
                 if surface_temp<-5 then
                    begin
                       world:=14;
                       wat_type:=6;
                    end;
                 if (surface_temp>35) and (water>19) then world:=22;
                 if (atmos=2) and (surface_temp>40) and (surface_temp<=60)then
                     begin
                        world:=22;
                        wat_type:=5;
                        water:=de(50,1)+50;
                        surface_temp:=surface_temp-20;
                     end;
                 if ((atmos=1) and (surface_temp>50)) or
                    ((atmos=2) and (surface_temp>60)) then
                     begin
                        world:=21;
                        wat_type:=1;
                        water:=0;
                     end;
                if (atmos=1) and (surface_temp>50) then
                     begin
                        world:=4;
                        wat_type:=1;
                        water:=0;
                     end;

          end;

        hydro[1]:= water;
        hydro[3]:= clouds;
        hydro[2]:= ice;
        hydro[4]:= albedo;

    end;



{ ----------------------------------------
      retuns the mineral ressources
       diametre=-1 asteroid belt
       diametre=0  ring
  ---------------------------------------- }

procedure mine_calcul (diametre:longint;density:byte;var mine:table_num);
   const mine_k : ARRAY [1..5] of byte =
      (70,40,10,30,10);
   var aux,a:smallint;
    aux1 :single;
   begin
     if diametre=0 then
	begin
	  diametre:=700;
	  density:=de(10,1)+3;
	end;
     if (diametre=-1) and (density>9) then diametre:=5000
        else if (diametre=-1) and (density<10) then diametre:=3000 ;
     for a:=1 to 5 do
        begin
          aux1:=diametre*density/2000;
          if aux1>120 then aux1:=120;
          aux:=trunc((aux1)+de(mine_k[a],1)-60);
          if aux<1 then aux:=1;
          if aux>mine_k[a] then aux:=mine_k[a]+trunc((aux-mine_k[a])/5);
          if aux>100 then aux:=100;
          mine[a]:=aux;
        end;
   end;


procedure read_ini_files;
   var
        WinIni: TIniFile;
        exe_path: string;
   begin
        exe_path:=ExtractFilePath(ParamStr(0));
        WinIni := TIniFile.Create(concat(exe_path,'starwin.ini'));
        with WinIni do
        begin
           star_prob[1]:=ReadInteger('Stellar Probabilities', 'Pheno', 1);
           star_prob[2]:=ReadInteger('Stellar Probabilities', 'Misc', 5)+star_prob[1];
           star_prob[3]:=ReadInteger('Stellar Probabilities', 'A_II', 9)+star_prob[2];
           star_prob[4]:=ReadInteger('Stellar Probabilities', 'M_III', 20)+star_prob[3];
           star_prob[5]:=ReadInteger('Stellar Probabilities', 'F_IV', 30)+star_prob[4];
           star_prob[6]:=ReadInteger('Stellar Probabilities', 'F_V', 115)+star_prob[5];
           star_prob[7]:=ReadInteger('Stellar Probabilities', 'G_V', 250)+star_prob[6];
           star_prob[8]:=ReadInteger('Stellar Probabilities', 'K_V', 260)+star_prob[7];
           star_prob[9]:=ReadInteger('Stellar Probabilities', 'M_V', 260)+star_prob[8];
           star_prob[10]:=ReadInteger('Stellar Probabilities', 'F_VII', 50)+star_prob[9];
        end;
        WinIni.Free;
   end;



{ -----------------------------------------
  returns the number of stars in the system,
  their spectral class,luminosity,mass,age,
  and companion star orbit radii.
  ----------------------------------------- }

procedure star_creation(var luminosity,mass:table_2;var age:byte;
    var spe_class,type_spec:table_1;var star_nbr:byte;var distance:table_0;
    gaia:boolean);
   var b:byte;
       aux:word;
       main_sequence_life:single;
       err_mess:smallint;
   begin
           aux:=de(10,1);
           case aux of
              1..4: star_nbr:=1;
              5..8: star_nbr:=2;
              9..10: star_nbr:=3;
           end;
           for b:=1 to 3 do luminosity[b]:=0;
	   for b:=1 to star_nbr do
	    begin
              aux:=de(1000,1)+(b-1)*200;
              if aux>1000 then aux:=1000;
              if aux<=star_prob[1] then { Phenomenae }
                      begin
                         case de(10,1) of
                           1: spe_class[b]:=22;
                           2: spe_class[b]:=16;
                           3: spe_class[b]:=17;
                           4: spe_class[b]:=18;
                           5..7: spe_class[b]:=19;
                           else spe_class[b]:=15;
                         end;
                      end
                  else
                   if aux<=star_prob[2] then { Misc stars }
                      begin
                         case de(5,1) of
                           1: spe_class[b]:=10;
                           2: spe_class[b]:=11;
                           3: spe_class[b]:=12;
                           4: spe_class[b]:=13;
                           else spe_class[b]:=8;
                         end;
                      end
                  else
                    begin
                       if aux<= star_prob[3] then spe_class[b]:=1
                       else if aux<=star_prob[4] then  spe_class[b]:=2
                       else if aux<=star_prob[5] then  spe_class[b]:=3
                       else if aux<=star_prob[6] then  spe_class[b]:=4
                       else if aux<=star_prob[7] then  spe_class[b]:=5
                       else if aux<=star_prob[8] then  spe_class[b]:=6
                       else if aux<=star_prob[9] then  spe_class[b]:=7
                       else spe_class[b]:=9;
                    end;

	      if (star_nbr>1) and (spe_class[1]>spe_class[b]) then
		  spe_class[b]:=spe_class[1]+1;
	      type_spec[b]:=de(10,1)-1;
	      case spe_class[b] of
	      1: begin
		  mass[b]:=14-0.3*type_spec[b];
		  luminosity[b]:=2200-type_spec[b]*160;
		 end;
	      2: begin
		  mass[b]:=6.3+0.31*type_spec[b];
		  luminosity[b]:=470+22*type_spec[b];
		 end;
              3: begin
		  mass[b]:=2.5-type_spec[b]*0.05;
		  luminosity[b]:=19-type_spec[b]*0.7;
                 end;
	      4: begin
		  mass[b]:=1.7-0.07*type_spec[b];
		  luminosity[b]:=8.1-type_spec[b]*0.7;
		 end;
	      5: begin
		  mass[b]:=1.04-0.02*type_spec[b];
		  luminosity[b]:=1.05-type_spec[b]*0.025;
		 end;
	      6: begin
		  mass[b]:=0.82-0.034*type_spec[b];
		  luminosity[b]:=0.42-type_spec[b]*0.038;
		 end;
	      7: begin
		  mass[b]:=0.48-0.027*type_spec[b];
		  luminosity[b]:=0.04-0.0039*type_spec[b];
		 end;
              8: begin
                    mass[b]:=0.15-type_spec[b]*0.01;
                    luminosity[b]:=0.011-type_spec[b]*0.0011;
                 end;
	      9: begin
		  mass[b]:=0.8;
                  type_spec[b]:=0;
		  luminosity[b]:=0.001;
		 end;
              10: begin
                   mass[b]:=16+type_spec[b]*1;
                   luminosity[b]:=46000+type_spec[b]*750;
                  end;
              11: begin
                    mass[b]:=30-type_spec[b]*1.7;
                    luminosity[b]:=170000-type_spec[b]*16800;
                  end;
              12: begin
		   mass[b]:=20+type_spec[b]*1;
		   luminosity[b]:=117000+type_spec[b]*2400;
                  end;
              13: begin
		   mass[b]:=30+type_spec[b]*3;
		   luminosity[b]:=200000+type_spec[b]*30000;
                  end;
              15: begin
                   luminosity[b]:=de(20,1); {diameter in Pc}
                  end;
              else begin
                      mass[b]:=0;
                      type_spec[b]:=0;
                   end;
	     end;
             if luminosity[b]<0 then
                err_mess:=Application.MessageBox('Error 3 (luminosity)','Error',0);
             aux:=de(6,1);
             if b>1 then
                begin
                  if (aux=1) or (mass[1]>2) then
                       distance[b-1]:=de(950,1)+50
	             else distance[b-1]:=de(90,1)+10;
                  if mass[1]>10 then distance[b-1]:=de(700,1)+300;
                  if gaia and (distance[b-1]<5) then distance[b-1]:=50;
                end;
	 end;
      if (spe_class[1]>7) or (spe_class[1]=1) then star_nbr:=1;
      case spe_class[1] of
       1..12: begin
                 main_sequence_life:=1000*(mass[1]/luminosity[1]);
                 if (main_sequence_life>=6) then age:=random(50)+10
	             else age:=random(10)+1;
              end;
       13   : age:=de(3,1);
       15   : age:=de(5,1);
       else   age:=de(10,1);
     end;
   end;

{ --- returns the number of planets around each star ----
      astral_orbit<0 => asteroid belt                    }

procedure orbit_creation(var astral_orbit:table_5;var planet_nbr:byte;
	      stellar_type:byte;mass,companion_dist,lux:single;
              current_star,star_nbr:byte;gaia:boolean);
  var a,aux,planet_tmp:byte;
      orbit_tmp:single;
    begin
       astral_orbit[1,current_star]:=de(9,1);
       if astral_orbit[1,current_star]<(sqrt(lux)/10) then      { untenable orbits }
               astral_orbit[1,current_star]:=trunc(sqrt(lux)/10)+1;
       if astral_orbit[1,current_star]<(3.8*power(mass,1/3)) then
               astral_orbit[1,current_star]:=trunc(3.8*power(mass,1/3));
       if astral_orbit[1,current_star]=0 then astral_orbit[1,current_star]:=1;
       orbit_tmp:=astral_orbit[1,current_star];
       case stellar_type of
	  1,2,3,8,9,10,11,12,16,17: planet_nbr:=0;
	  4: if not gaia then planet_nbr:=de(10,1) else planet_nbr:=de(8,1)+2;
	  5: if not gaia then planet_nbr:=de(6,3) else planet_nbr:=de(4,4)+2;
	  6: planet_nbr:=de(6,2);
	  7: planet_nbr:=de(6,1);
       end;
      planet_tmp:=planet_nbr;
      for a:=2 to planet_tmp do
	 begin
	  aux:=de(10,1);
	  if (aux=1) then
	       begin
		  if (de(6,1)>2) then
			  begin
			    astral_orbit[a,current_star]:=-round(orbit_tmp*(de(9,1)/10+1.1));
                            if astral_orbit[a,current_star]>-2 then
                               astral_orbit[a,current_star]:=-2;
			    orbit_tmp:=abs(astral_orbit[a,current_star]);
			  end
		    else  begin
			    planet_nbr:=a-1;
                            system.break;
			  end
	       end
	     else  begin
                      astral_orbit[a,current_star]:=round(orbit_tmp*(aux/10+1.1));
                      if astral_orbit[a,current_star]=1 then
                         astral_orbit[a,current_star]:=2;
		      orbit_tmp:=astral_orbit[a,current_star];
		   end;
	 if (orbit_tmp>400*mass) or ((star_nbr>1) and
            (orbit_tmp>companion_dist/3)) then
				begin
				  planet_nbr:=a-1;
                                  system.break;
				end;
	 end;
    end;

procedure stardll_ini;   {Needs this for a new random seed for each new sector}
begin
  randomize;
end;


END.