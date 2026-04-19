{ Star for Windows v1.6 a system generator by Aina Rasolomalala (c)
   beta   11-1998
   v1.55  03-1999  32-bit version
   v1.56a 07-1999  add compatibility with Empire Generator
   v1.56b 01-2000  more infos for asteroid belts/ Delphi 3 code
   v1.57  01-2000  max_spatial parameter
   v1.57a 01-2000  compatiblity with Empire v1.57a
   v1.57b 05-2000  phenomenae factor
   v1.6   01-2001  new slot for 8 planet faclities}

unit Star;

interface

uses recunit;

procedure main_star (outstr:string;new_file,gaia:boolean;vpos_x,vpos_y,vpos_z:
    smallint;al_disable:boolean;system_name:string50;max_spatial:byte;phenomenae:boolean);
    export;
procedure planet_creation (lux:single;var sys1:star_record;
    var sys2:planet_record;current_star:byte; var id_count:longint;
    var pfile: planet_record_file;var mfile:moon_record_file;outstr,datafile:string;
    id_sun:longint;al_disable:boolean;max_spatial:byte);export;
procedure moon_creation (var twin,roche:boolean;sys2:planet_record;innerzone,
    lifezone,outerzone:smallint;planet_id:longint;lux:single;datafile2:string;
    planet_mass,planet_gravity:single);export;


implementation

uses procunit,Alien,Forms,sysUtils;


var gaia:boolean;



procedure emptystr(var f:text;a:byte);
var aux:byte;
begin
for aux:=1 to a do write(f,' ');
end;

{ ----------------- Gives the infos on the moons -------------------}

procedure moon_creation (var twin,roche:boolean;sys2:planet_record;innerzone,
    lifezone,outerzone:smallint;planet_id:longint;lux:single;datafile2:string;
    planet_mass,planet_gravity:single);
var sys3: moon_record;
    mfile : file of moon_record;
    aux:smallint;
    id_count: longint;
    volatile_gas_inventory,escape_velocity:single;
    molecule_weight:byte;
    gravity,mass,tmp:single;
begin
   assign(mfile,datafile2);
   reset(mfile);
   seek(mfile,filesize(mfile));
   id_count:=filesize(mfile);
   for aux:=1 to sys2.satellites do
       begin
         sys3.name:=''; {v1.6 addition}
         if (de(1000,1)=1) and (sys2.satellites=1) then  {Twin Worlds}
         begin
           if is_set(sys2.misc_charac,1) then set_bit(sys3.misc_charac,1)
              else unset_bit(sys3.misc_charac,1);
           sys3.diametre:=sys2.diametre;
           sys3.density:=sys2.density;
           mass:=planet_mass;
           gravity:=planet_gravity;
           sys3.pressure:=sys2.pressure;
           sys3.atmos_type:=sys2.atmos_type;
           sys3.world_type:=sys2.world_type;
           sys3.water_type:=sys2.water_type;
           sys3.hydrography:=sys2.hydrography;
           sys3.temp_avg:=sys2.temp_avg;
           sys3.atmos:=sys2.atmos_type;
           mine_calcul(sys3.diametre,sys3.density,sys3.mine_ress);
           if de(10,1)>1 then
              begin
                tmp:=moon_orbit(sys3.diametre)*sys2.diametre/1000;
                sys3.sat_orbit:=trunc(tmp);
                twin:=true;
                roche:=false;
              end
              else begin
                     sys3.sat_orbit:=trunc(1.225*sys2.diametre/1000);
                     twin:=false;
                     roche:=true;
                   end;
         end
         else
         begin
           twin:=false;
           roche:=false;
	   if (de(6,1)>1) and (sys2.orbit_radius>outerzone) then
                             set_bit(sys3.misc_charac,1)
	         else unset_bit(sys3.misc_charac,1);
           sys3.diametre:=(de(19,1)-6)*1000;      
	   if sys3.diametre<0 then sys3.diametre:=de(1000,1)
	         else if sys3.diametre<>0 then
                      sys3.diametre:=sys3.diametre+de(999,1);
	   while (1.3*sys3.diametre>sys2.diametre) do
	         sys3.diametre:=round(sys3.diametre/2);
	   if is_set(sys3.misc_charac,1) then sys3.density:=de(6,1)
	         else sys3.density:=de(10,1)+3;
	   gravity_calcul(sys3.density,sys3.diametre,mass,
                        gravity,escape_velocity);
	   if sys3.diametre<>0 then atmos_calcul(sys3.atmos_type,innerzone,lifezone,outerzone,
                 sys2.orbit_radius,mass,gravity,sys3.pressure,
                 volatile_gas_inventory,sys3.diametre,is_set(sys3.misc_charac,1),
                 escape_velocity,molecule_weight);
	   world_calcul(sys3.atmos_type,sys3.world_type,innerzone,
                 outerzone,lifezone,sys2.orbit_radius,sys3.diametre,
                 is_set(sys3.misc_charac,1),gaia);
           if sys3.world_type=19 then
               begin
                  sys3.pressure:=0;
                  sys3.atmos_type:=6;
                  sys3.atmos:=1;
               end;
	   Determine_albedo_and_surface_temp(innerzone,outerzone,sys2.orbit_radius,
                 sys3.world_type,sys3.water_type,sys3.hydrography,
                 volatile_gas_inventory,lux,gravity,sys3.pressure,
                 sys3.diametre,sys3.atmos_type,sys3.temp_avg,escape_velocity);
           tmp:=moon_orbit(sys3.diametre)*sys2.diametre/1000;
           sys3.sat_orbit:=trunc(tmp);
	   sys3.atmos:=compos_atmos(sys3.atmos_type,sys3.world_type,
                 gravity,sys3.pressure,escape_velocity,lifezone,
                 sys2.orbit_radius,molecule_weight);
           mine_calcul(sys3.diametre,sys3.density,sys3.mine_ress);
           if planet_mass>3180 then lux:=lux+0.1;
         end;
         sys3.pln_id:=planet_id;
         id_count:=id_count+1;
         write(mfile,sys3);
      end;
   close(mfile);
end;

procedure planet_creation (lux:single;var sys1:star_record;
    var sys2:planet_record;current_star:byte; var id_count:longint;
    var pfile: planet_record_file;
    var mfile:moon_record_file;outstr,datafile:string;id_sun:longint;
    al_disable:boolean;max_spatial:byte);
var innerzone,outerzone,lifezone:smallint;
    afile:alien_record_file;
    comp_dist,volatile_gas_inventory,escape_velocity:single;
    a,c:byte;
    aux:smallint;
    sys3: moon_record;
    day_temp,orbit_period,rot,max_day:longint;
    twin,roche:boolean;
    mass,gravity,rot_lux:single;
    temp:smallint;
begin
       innerzone:=round(8.2*sqrt(lux));
       lifezone:=round(10*sqrt(lux));
       outerzone:=round(12*sqrt(lux));
       if lifezone=0 then lifezone:=1;
       if sys1.companion_dist[1]>sys1.companion_dist[2] then
          comp_dist:=sys1.companion_dist[2]
          else comp_dist:=sys1.companion_dist[1];
       orbit_creation(sys1.astral_orbit,sys1.planet_nbr[current_star],
               sys1.spe_class[current_star],sys1.mass_star[current_star],
               comp_dist,lux,current_star,sys1.star_nbr,gaia);
       sys1.pln_id[current_star]:=id_count;
       sys2.allegiance:=65535;
       for a:=1 to sys1.planet_nbr[current_star] do
	   begin
             sys2.name:=''; {v1.6 addition}
             sys2.misc_charac:=0;
             sys2.orbit_radius:=sys1.astral_orbit[a,current_star];
             if sys2.orbit_radius<0 then    { asteroid belts }
		begin
		  sys2.orbit_radius:=abs(sys1.astral_orbit[a,current_star]);
		  aux:=de(6,2)-1;
		  if (sys2.orbit_radius<15) then aux:=aux-3
		  else if (sys2.orbit_radius<200) then aux:=aux-1;
		  if (aux<1) then aux:=1;
                  sys2.diametre:=aux;
		  aux:=de(6,2);
		  if sys2.orbit_radius <= innerzone then aux:=aux-4;
		  if sys2.orbit_radius >= outerzone then aux:=aux+2;
		  if aux<5 then sys2.world_type:=15
		  else if aux<8 then sys2.world_type:=16
		       else if aux<12 then sys2.world_type:=17
			    else sys2.world_type:=18;
		  case sys2.world_type of
		      15: begin
                            sys2.density:=14;
                            sys2.hydrography[4]:=15;
                          end;
		      16: begin
                            sys2.density:=6;
                            sys2.hydrography[4]:=10;
                          end;
		      17: begin
                            sys2.density:=4;
                            sys2.hydrography[4]:=5;
                          end;
		      18: begin
                            sys2.density:=2;
                            sys2.hydrography[4]:=35;
                          end;  
		  end;
		  mine_calcul(-1,sys2.density,sys2.mine_ress);
                  sys2.inclination:=orbital_inclination_calcul(sys2.orbit_radius);
                  sys2.temp_avg:=temp_calcul(lux,sys2.orbit_radius,
                       1,sys2.world_type,7);
                  sys2.sun_id:=id_sun;
                  for aux:=1 to 5 do sys2.unusual[aux]:=0;
                  sys2.atmos_type:=6;
                  sys2.atmos:=1;
                  sys2.satellites:=0;
                  if sys2.world_type=18 then
                     begin
                          sys2.water_type:=3;
                          sys2.hydrography[2]:=0;
                     end
                     else sys2.water_type:=1;
                  id_count:=id_count+1;
                  write(pfile,sys2);
		end
	     else begin
		    twin:=false;
                    roche:=false;
                    if sys2.orbit_radius=0 then sys2.orbit_radius:=1;  {to fix a bug, don't know where it's created}
		    diametre_calcul(sys2.misc_charac,sys2.orbit_radius,innerzone,
                       outerzone,sys2.diametre,sys2.density,gaia);
		    gravity_calcul(sys2.density,sys2.diametre,mass,
                       gravity,escape_velocity);
                    atmos_calcul(sys2.atmos_type,innerzone,lifezone,outerzone,
                       sys2.orbit_radius,mass,gravity,sys2.pressure,
                       volatile_gas_inventory,sys2.diametre,
                       is_set(sys2.misc_charac,1),escape_velocity,sys2.smwr);
		    world_calcul(sys2.atmos_type,sys2.world_type,innerzone,
                       outerzone,lifezone,sys2.orbit_radius,sys2.diametre,
                       is_set(sys2.misc_charac,1),gaia);
		    Determine_albedo_and_surface_temp(innerzone,outerzone,
                       sys2.orbit_radius,sys2.world_type,sys2.water_type,
                       sys2.hydrography,volatile_gas_inventory,lux,
                       gravity,sys2.pressure,sys2.diametre,
                       sys2.atmos_type,sys2.temp_avg,escape_velocity);
		    sys2.atmos:=compos_atmos(sys2.atmos_type,sys2.world_type,
                       gravity,sys2.pressure,escape_velocity,lifezone,
                       sys2.orbit_radius,sys2.smwr);
		    sys2.eccentricity:=eccentricity_calcul;
		    orbit_period:=orbit_calcul(sys2.orbit_radius,
                        sys1.mass_star[current_star]);
                    if mass>3180 then sys2.world_type:=20;
		    sys2.satellites:=satellites_calcul(sys2.world_type);

		    rotation_calcul(sys2.orbit_radius,mass,
                       gravity,sys2.diametre,sys1.mass_star[current_star],
                       sys2.eccentricity,sys2.satellites,sys2.water_type,
                       orbit_period,sys2.world_type,sys2.misc_charac,
                       sys2.rotation_period,sys2.density,sys1.age);
		    mine_calcul(sys2.diametre,sys2.density,sys2.mine_ress);
		    escape_velocity:=escape_velocity_calcul(mass,
                       sys2.diametre);
                    sys2.inclination:=orbital_inclination_calcul(sys2.orbit_radius);
		    sys2.axial_tilt:=axial_tilt_calcul;
		    if (sys2.world_type=3) or (sys2.world_type=20) then
                          for c:=1 to 5 do sys2.mine_ress[c]:=0;
		    if (sys2.atmos>2) and (sys2.hydrography[1]>0) then
                        begin
                          if de(6,2)>10 then set_bit(sys2.misc_charac,3)
                            else unset_bit(sys2.misc_charac,3);
                        end;

                    rot_lux:=lux/sqrt(sys2.orbit_radius/10);
	            rot:=abs(sys2.rotation_period);
	            temp:=sys2.temp_avg+273;
	            day_temp:=trunc(day_table[sys2.atmos_type]*rot_lux*rot/2);
	            max_day:=trunc(temp*day_max[sys2.atmos_type]*rot_lux);
	            if day_temp>max_day then day_temp:=max_day;
                    if (day_temp+temp)>2000 then  day_temp:=2000-temp;

                    if sys2.satellites>0 then
                        begin
                          assign(mfile,datafile);
                          reset(mfile);
                          sys2.moon_id:=filesize(mfile);
                          close(mfile);
                          moon_creation(twin,roche,sys2,innerzone,lifezone,
                             outerzone,id_count,lux,datafile,mass,gravity);
                        end;
                    unusual_calcul(sys2.atmos_type,sys2.world_type,sys2.satellites,
                       sys2.water_type,sys2.hydrography[1],sys2.axial_tilt,
                       sys2.density,sys2.atmos,sys2.mine_ress,sys2.rotation_period,
                       sys2.misc_charac,twin,roche,sys2.eccentricity,
                       sys2.temp_avg,day_temp,gravity,mass,sys2.unusual,
                       sys2.mine_ress[2],sys2.hydrography[3]);
                    if  is_set(sys2.unusual[3],3) then
                      begin
                       assign (afile,concat(outstr,'.aln'));
                       reset(afile);
                       sys2.alien_id:=filesize(afile);
                       close(afile);
                       main_alien (sys2.temp_avg,gravity,sys2.atmos,
                            sys2.world_type,false,outstr,id_count,al_disable,max_spatial);
                      end;
                    sys2.sun_id:=id_sun;
                    id_count:=id_count+1;
                    write(pfile,sys2);
	     end;
   end;
end;


procedure path_ini;
begin
{$I-}
ChDir('data');
if IOResult <> 0 then MkDir('data')
   else ChDir('..');
{$I+}
{$I-}
ChDir('log');
if IOResult <> 0 then MkDir('log')
    else ChDir('..');
{$I+}
end;


procedure main_star (outstr:string;new_file,gaia:boolean;vpos_x,vpos_y,vpos_z:
    smallint;al_disable:boolean;system_name:string50;max_spatial:byte;phenomenae:boolean);
var sys1: star_record;
    sys2: planet_record;
    sys5: names_record;
    afile: alien_record_file;
    mfile: moon_record_file;
    pfile: planet_record_file;
    sfile: star_record_file;
    nfile: name_record_file;
    cyfile: colony_record_file;
    ctfile: contact_record_file;
    efile: empire_record_file;
    cfile: text;
    b:byte;
    lux:single;
    datafile:string;
    s_count:integer;
    id_count:longint;
const
    earth_change_velocity=-1.3E-15;
begin


 datafile:=concat(outstr,'.mon');
 assign (pfile,concat(outstr,'.pln'));
 assign (mfile,datafile);
 assign (sfile,concat(outstr,'.sun'));
 assign (afile,concat(outstr,'.aln'));
 assign (cfile,concat(outstr,'.cmt'));
 assign (nfile,concat(outstr,'.nam'));
 assign (cyfile,concat(outstr,'.col'));
 assign (ctfile,concat(outstr,'.con'));
 assign (efile,concat(outstr,'.emp'));
 if new_file then
    begin
            rewrite (pfile);
            rewrite (mfile);
            rewrite (sfile);
            rewrite (afile);
            rewrite (cfile);
            rewrite (nfile);
            rewrite (ctfile);
            rewrite (cyfile);
            rewrite (efile);
            sys5.name:=system_name;
            sys5.body_type:=0;  {to put software version in the *.nam files}
            sys5.body_Id:=8;    {v1.6}
            write(nfile,sys5);
            close   (cfile);
            close   (ctfile);
            close   (cyfile);
            close   (nfile);
            close(efile);
            if FileExists(concat(outstr,'.aln1')) then
              DeleteFile(concat(outstr,'.aln1'));
            if FileExists(concat(outstr,'.csv')) then
              DeleteFile(concat(outstr,'.csv'));
            if FileExists(concat(outstr,'_RP.csv')) then
              DeleteFile(concat(outstr,'_RP.csv'));
    end
    else begin
            {$I-}
            reset(pfile);
            reset(mfile);
            reset(afile);
            reset(sfile);
            reset(cfile);
            reset(nfile);
            {$I+}
            if IoResult<>0 then begin                {files don't exist}
                                   rewrite(pfile);
                                   rewrite(sfile);
                                   rewrite(mfile);
                                   rewrite(afile);
                                   rewrite(nfile);
                                   rewrite (ctfile);
                                   rewrite (cyfile);
                                   closefile (ctfile);
                                   closefile (cyfile);
                                   sys5.name:=system_name;
                                   sys5.body_type:=0;  {to put software version in the *.nam files}
                                   sys5.body_Id:=8;     {v1.6}
                                   write(nfile,sys5);
                                   rewrite(cfile);
                                 end;
            seek(pfile,filesize(pfile));
            seek(sfile,filesize(sfile));
            close(cfile);
            close(nfile);
          end;
 close(mfile);
 close(afile);
 id_count:=filesize(pfile);
 s_count:=filesize(sfile);
 star_creation(sys1.luminosity,sys1.mass_star,sys1.age,
               sys1.spe_class,sys1.type_spec,sys1.star_nbr,
               sys1.companion_dist,gaia);
 sys1.posX:=vpos_x;
 sys1.posY:=vpos_y;
 sys1.posZ:=vpos_z;
 sys1.planet_nbr[2]:=0;sys1.planet_nbr[3]:=0;
 sys1.name:='';
 for b:=1 to sys1.star_nbr do
     begin
       sys1.planet_nbr[b]:=0;
       lux:=sys1.luminosity[b];
       case sys1.star_nbr of
            2: if b=1 then lux:=lux+sys1.luminosity[2]/(sqrt(sys1.companion_dist[1]/10))
	               else lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[1]/10));
            3: if b=1 then lux:=lux+sys1.luminosity[2]/(sqrt(sys1.companion_dist[1]/10))
                       +sys1.luminosity[3]/(sqrt(sys1.companion_dist[2]/10))
                 else if b=2 then lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[1]/10))+
                       sys1.luminosity[3]/(sqrt((sys1.companion_dist[1]+sys1.companion_dist[2])/10))
                 else lux:=lux+sys1.luminosity[1]/(sqrt(sys1.companion_dist[2]/10))+
                       sys1.luminosity[2]/(sqrt((sys1.companion_dist[1]+sys1.companion_dist[2])/10));
       end;
       planet_creation (lux,sys1,sys2,b,id_count,pfile,mfile,outstr,
          datafile,s_count,al_disable,max_spatial);
    end;
 sys1.allegiance:=65535;
 sys1.code:=0;
 write(sfile,sys1);
 close(pfile);
 close(sfile);
end;

end.
