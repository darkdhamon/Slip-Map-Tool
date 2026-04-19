{ Star Generator Logfile for Windows v1.6 by Aina Rasolomalala (c) 02-2001

   v1.4   New colony infos
   v1.57b New stellar classes
   v1.6   New facilities compatible, Html output}


unit starlog;

interface

procedure main_starlog (filename,outstr:string;id:longint;rpg_output,
   output_form:byte);export;

implementation

uses starunit,recunit,procunit,forms,alienlog,printers;

var hr_str,b1,b2: string;

procedure file_init(var sizefile:integer;outstr:string;var error_result:boolean);
var pfile: file of planet_record;
    sfile: file of star_record;
    mfile: file of moon_record;
    error_mess:smallint;
begin
{$I-}
 error_result:=true;
 assign (pfile,concat(outstr,'.pln'));
 assign (sfile,concat(outstr,'.sun'));
 assign (mfile,concat(outstr,'.mon'));
 reset(sfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('sun file empty or doesn'' exist',
         'Error', 0);
       close(sfile);
       Exit;
    end;
 sizefile:=filesize(sfile);

 reset(pfile);
 if IoResult<>0 then
    begin
      error_mess:=Application.MessageBox('pln file doesn''t exist','Error', 0);
      close(pfile);
      Exit;
    end;
 reset(mfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('mon file doesn''t exist','Error', 0);
       close(mfile);
       Exit;
    end;
 close(pfile);
 close(sfile);
 close(mfile);
 error_result:=false;
{$I+}
end;

procedure emptystr(var f:text;a:smallint);
var aux:smallint;
begin
for aux:=1 to a do write(f,' ');
end;


procedure unusual_display(var f:text; unusual:table_bool);
var count:boolean;
    aux:smallint;
    tmp:byte;
begin
     writeln(f,'Unusual Characteristics:');
     count:=false;
     for aux:=1 to max_unusual do
         begin
           tmp:=1+trunc((aux-1)/8);
           if is_set(unusual[tmp],aux-(tmp-1)*8) then
              begin
                 writeln(f,'  -',unusual_genre[aux]);
                 count:=true;
              end;
          end;
      if not count then writeln(f,'  -None');
      writeln(f);
      writeln(f);

end;


procedure sun_display(var f:text;sys1:star_record;b:smallint;filename,
    outstr:string;output_form:byte);
var a: smallint;
    pfile:file of planet_record;
    sys2:planet_record;
    mass,escape_velocity,gravity:single;
begin
       assign (pfile,concat(filename,'.pln'));
       reset(pfile);
       writeln(f,hr_str);
       if b>1 then write(f,class_spec[21])
       else write(f,class_spec[20]);

       writeln(f);
       if sys1.spe_class[1]<15 then
         begin
           write(f,'    Luminosity    : ');
           if sys1.luminosity[b]<0.001 then writeln(f,sys1.luminosity[b]:0:4)
	      else writeln(f,sys1.luminosity[b]:0:3);
           writeln(f,'    Star Mass     : ',sys1.mass_star[b]:0:3);
           write(f,'    Spectral class: ',class_spec[sys1.spe_class[b]]);
           case sys1.spe_class[b] of
                1,11: writeln(f,sys1.type_spec[b],' II');
                2   : writeln(f,sys1.type_spec[b],' III');
                3   : writeln(f,sys1.type_spec[b],' IV');
                4..7: writeln(f,sys1.type_spec[b],' V');
                8   : writeln(f,sys1.type_spec[b],' VI');
                9   : writeln(f,' VII');
                12,13: writeln(f,sys1.type_spec[b],' Ia');
                10  : writeln(f,sys1.type_spec[b],' Ib');
           end;
         end
         else writeln(f,class_spec[sys1.spe_class[b]]);
       if sys1.spe_class[1]=15 then
          writeln(f,'    Diameter      : ',sys1.luminosity[1]:3:0,' Parsecs');
       writeln(f,'    Age           : ',sys1.age/10:0:1, ' billions years');
       writeln(f,hr_str);
       writeln(f);
       writeln(f,'Planets present at:');
       for a:=1 to sys1.planet_nbr[b] do
         begin
           if output_form=2 then write(f,'<a HREF="#',b,'_',a,'">');
	   write(f,a:2);
           if output_form=2 then write(f,'</a>');
           write(f,'  ',abs(sys1.astral_orbit[a,b]/10):4:1,' A.U.');
           seek(pfile,sys1.pln_id[b]-1+a);
           read(pfile,sys2);
           write(f,'  ',world_genre[sys2.world_type]);
           gravity_calcul(sys2.density,sys2.diametre,mass,
                        gravity,escape_velocity);
           write(f,'  ',gravity:0:2,' gees');
           write(f,'  ',sys2.temp_avg:4, ' °C');
           if is_set(sys2.unusual[3],3) then write(f,'  ',unusual_genre[19]);
           writeln(f);
         end;
       if sys1.planet_nbr[b]=0 then writeln(f,' no planets');
       writeln(f);
       close(pfile);
end;

procedure col_display(var f:text;sys7:colony_record;output_form:byte;filename:string);
var multiplier_pop:single;
    factor_pop,a,tmp:byte;
    total_pop:longint;
    facilities:boolean;
    aux:string;
    afile: alien_record_file;
    sys4: alien_record;
const pop_index: ARRAY [1..7] of single=
   (0.01,0.1,1,10,100,1000,10000);
begin
   assign (afile,concat(filename,'.aln'));
   reset(afile);
   if output_form=2 then
        writeln(f,'<table border><tr><td bgcolor="#d6d6d6">');
   writeln(f,b1,'Colony:',b2);
   if output_form=2 then writeln(f,'</td><tr><td><pre>') else writeln(f,'-------');
   writeln(f,'  Type      : ',colony_genre[sys7.col_class]);
   seek(afile,sys7.race);
   read(afile,sys4);
   writeln(f,'  Race      : ',sys4.name,' [ ',sys7.race,' ]');
   write(f,'  Allegiance: ');
   if sys7.allegiance=65535 then writeln(f,'Independant')
      else
        begin
           seek(afile,sys7.allegiance);
           read(afile,sys4);
           writeln(f,sys4.name,' [ ',sys7.allegiance,' ]');
        end;
   if (sys7.col_class<>7) and (sys7.col_class<>8) then
     writeln(f,'  Age       : ',sys7.age,' centuries');
   factor_pop:=trunc(sys7.pop/10);
   multiplier_pop:=sys7.pop-factor_pop*10;
   if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
   total_pop:=round((multiplier_pop+1)*pop_index[factor_pop+1]*1000);
   if total_pop>999 then writeln(f,'  Population: ',round(total_pop/1000),' millions')
      else writeln(f,'  Population: ',total_pop,' thousands');
   writeln(f,'  Pop. comp : ',sys7.pop_comp,'% natives');
   writeln(f,'  Starport  : ',starport_genre[sys7.starport]);
   Str(sys7.law,aux);
   case sys7.law of
     1   : aux:='No law    [ '+aux+' ]';
     2,3 : aux:='Low       [ '+aux+' ]';
     4..6: aux:='Moderate  [ '+aux+' ]';
     7,8 : aux:='High      [ '+aux+' ]';
     9,10: aux:='Extreme   [ '+aux+' ]';
   end;
   writeln(f,'  Law       : ',aux);
   Str(sys7.stability,aux);
   case sys7.stability of
     1   : aux:='Revolt    [ '+aux+' ]';
     2,3 : aux:='Low       [ '+aux+' ]';
     4..6: aux:='Moderate  [ '+aux+' ]';
     7,8 : aux:='High      [ '+aux+' ]';
     9,10: aux:='Very high [ '+aux+' ]';
   end;
   writeln(f,'  Stability : ',aux);
   Str(sys7.crime,aux);
   case sys7.crime of
     1   : aux:='No crime  [ '+aux+' ]';
     2,3 : aux:='Low       [ '+aux+' ]';
     4..6: aux:='Moderate  [ '+aux+' ]';
     7,8 : aux:='High      [ '+aux+' ]';
     9,10: aux:='Very high [ '+aux+' ]';
   end;
   writeln(f,'  Crime     : ',aux);
   writeln(f,'  GWP       : ',sys7.gnp,' MCr');
   writeln(f,'  Power     : ',sys7.power);
   writeln(f,'  Facilities: ');
   facilities:=false;
   for a:=1 to 16 do
      begin
        tmp:=1+trunc((a-1)/8);
        if is_set(sys7.misc_char[tmp],a-(tmp-1)*8) then
          begin
            facilities:=true;
            writeln(f,'    -',facilities_genre[a]);
          end;
      end;
   if not facilities then writeln(f,'    -None');
   writeln(f);
   writeln(f,'  Export    : ',eco_genre[sys7.eco_export]);
   writeln(f,'  Import    : ',eco_genre[sys7.eco_import]);
   writeln(f);
   if output_form=2 then
      begin
         writeln(f,'</pre></td></tr></table>');
         writeln(f);
      end
     else writeln(f,'------------------------------------------------');
   closefile(afile);
end;

procedure sat_display(var f:text;moon_id:longint;filename:string;
   var cyfile:colony_record_file);
var mfile: file of moon_record;
    sys3: moon_record;
    gravity,mass,escape_velocity:single;
    counter1:longint;
    sys7:colony_record;
begin
   assign (mfile,concat(filename,'.mon'));
   reset(mfile);
   seek(mfile,moon_id);
   read(mfile,sys3);
   if sys3.diametre=0 then write(f,'       NA')
   else write(f,'   ',sys3.diametre:6);
   emptystr(f,4);
   write(f,'  ',sys3.sat_orbit:5);
   emptystr(f,2);
   write(f,'   ',world_genre[sys3.world_type]);
   gravity_calcul(sys3.density,sys3.diametre,mass,
                        gravity,escape_velocity);
   write(f,'   ',gravity:0:2);
   write(f,'   ',atmos_genre[sys3.atmos_type]);
   write(f,'   ',sys3.mine_ress[1],'/',sys3.mine_ress[2],'/',sys3.mine_ress[3]);
   write(f,'/',sys3.mine_ress[4],'/',sys3.mine_ress[5]);
   if sys3.mine_ress[1]>9 then write(f,'  ',sys3.temp_avg:4)
      else  write(f,'   ',sys3.temp_avg:4);
   if is_set(sys3.misc_charac,4) then
         begin
            for counter1:=0 to (filesize(cyfile)-1) do
               begin
                  seek(cyfile,counter1);
                  read(cyfile,sys7);
                  if (sys7.body_type=2) and (sys7.world_id=moon_id) then
                      break;
                 end;
            write(f,'   ',colony_genre[sys7.col_class]);
         end;
   writeln(f);
   close(mfile);
end;

procedure pln_display(pln_id:longint;a,b:smallint;filename,outstr:string;
     sys1:star_record;current_star:byte;rpg_output,output_form:byte;var f:text);
var  sys2: planet_record;
     sys7:colony_record;
     pfile: file of planet_record;
     cyfile:colony_record_file;
     c,equator,polar:smallint;
     magnetic,oxygene_taux,gravity,mass,escape_velocity:single;
     boiling,day_temp,night_temp,orbit_period,counter1:longint;
begin
    assign (pfile,concat(filename,'.pln'));
    assign (cyfile,concat(filename,'.col'));
    reset(pfile);
    reset(cyfile);
    seek(pfile,pln_id);
    read(pfile,sys2);
    writeln(f);
    if output_form=2 then write(f,'<a NAME="#',b,'_',a,'"></a>');
    if output_form<>2 then write(f,'* ');
    write(f,b1,'Planet ',a:2);

    if sys2.name<>'' then write(f,' [ ',sys2.name, ' ]',b2,' (Id: ',pln_id, ')')
       else write(f,b2,' (Id: ',pln_id, ')');
    writeln(f);

    close(pfile);

    if (sys2.world_type>14) and (sys2.world_type<19) then    { asteroid belts }
       begin
          writeln(f,'Orbit radius          : ',abs(sys2.orbit_radius)/10:0:1,' A.U.');
          writeln(f,'Asteroid belt         : ',belt_width[sys2.diametre]:0:2,
		    ' A.U. width');
          writeln(f,'Orbital inclination   : ',sys2.inclination,' °');
	  writeln(f,'Predominate asteroids : ',
                     world_genre[sys2.world_type]);
	  writeln(f,'Mineral ressources    :');
          writeln(f,'  -Metal ore      : ',sys2.mine_ress[1]);
          writeln(f,'  -Radioactive ore: ',sys2.mine_ress[2]);
          writeln(f,'  -Precious metal : ',sys2.mine_ress[3]);
          writeln(f,'  -Raw crystals   : ',sys2.mine_ress[4]);
          writeln(f,'  -Precious gems  : ',sys2.mine_ress[5]);
           writeln(f);
           writeln(f);
        end
    else begin
          writeln(f,'Orbit radius        : ',sys2.orbit_radius/10:0:1,' A.U.');
	  writeln(f,'Type                : ',world_genre[sys2.world_type]);
	  writeln(f,'Density             : ',sys2.density/10:0:1,'  Earth density');
	  writeln(f,'Diameter            : ',sys2.diametre,' km');
          gravity_calcul(sys2.density,sys2.diametre,mass,
                        gravity,escape_velocity);
	  writeln(f,'Gravity             : ',gravity:0:2,' Earth gees');
	  writeln(f,'Mass                : ',mass:0:3,'  Earth masses');
	  writeln(f,'Escape velocity     : ',escape_velocity/1E5:0:2,' km/s');
	  writeln(f,'Atmosphere          : ',atmos_genre[sys2.atmos_type]);
          if sys2.pressure>=900 then writeln(f,'  -Pressure         : Very High')
             else writeln(f,'  -Pressure         : ',sys2.pressure:0:3,' atm');
	  oxygene_taux:=oxygene_calcul(sys2.hydrography[2],
                       sys2.hydrography[1],sys2.atmos,gravity);
	  writeln(f,'  -Oxygen           : ',oxygene_taux:0:3,' atm');
	  writeln(f,'  -Composition      : ',atmos_comp[sys2.atmos]);
          if sys2.atmos<>6 then writeln(f,'  -Molecule limit   : ',sys2.smwr);
	  if (sys2.world_type<>3) and (sys2.world_type<>20) then
              begin
                 writeln(f,'Mineral ressources  :');
                 writeln(f,'  -Metal ore        : ',sys2.mine_ress[1]);
                 writeln(f,'  -Radioactive ore  : ',sys2.mine_ress[2]);
                 writeln(f,'  -Precious metal   : ',sys2.mine_ress[3]);
                 writeln(f,'  -Raw crystals     : ',sys2.mine_ress[4]);
                 writeln(f,'  -Precious gems    : ',sys2.mine_ress[5]);
              end;
	  write(f,'Water               : ',water_genre[sys2.water_type]);
	  if (sys2.atmos>2) and (sys2.hydrography[1]>0) then
             begin
               if is_set(sys2.misc_charac,3) then
                     write (f,' of tainted liquid water')
		  else write (f,' of atmosphere related chemical mix');
             end;
	  writeln(f);
	  writeln(f,'  %Water            : ',sys2.hydrography[1]);
          writeln(f,'  %Ice              : ',sys2.hydrography[2]);
          writeln(f,'  %Clouds           : ',sys2.hydrography[3]);
          writeln(f,'  Albedo            : ',sys2.hydrography[4]/100:0:2);
          if sys2.pressure=0 then boiling:=-273
          else boiling:=trunc(1/(Ln(sys2.pressure)/-5050.5+ 1.0/373)-273);
          writeln(f,'  Boiling Point     : ',boiling,' °C');
          orbit_period:=orbit_calcul(sys2.orbit_radius,
                        sys1.mass_star[current_star]);
	  writeln(f,'Orbital  period     : ',orbit_period,' days');
	  writeln(f,'Rotation period     : ',sys2.rotation_period,' hours');
          writeln(f,'Orbital inclination : ',sys2.inclination,' °');
	  writeln(f,'Eccentricity        : ',sys2.eccentricity/1000:0:3);
	  writeln(f,'Axial tilt          : ',sys2.axial_tilt,' °');
          magnetic:=magnetic_calcul(is_set(sys2.unusual[3],1),mass,sys2.density,sys2.world_type,
            sys2.rotation_period);
          writeln(f,'Magnetic field      : ',magnetic:0:2,' gauss');
	  writeln(f,'Temperature:');
	  writeln(f,'  -Average Temperature      : ',sys2.temp_avg,' °C');
	  writeln(f,'  -Effects of eccentricity  : +/-',
		      trunc(sys2.eccentricity*0.03),' °C');
	  writeln(f,'  -Maximum increase         : +',
			 sys2.axial_tilt*0.6:0:0,' °C');
	  writeln(f,'  -Maximum decrease         : -',
			 sys2.axial_tilt/1.5:0:0,' °C');
	  temp_var(day_temp,night_temp,sys2.orbit_radius,
                        sys2.atmos_type,sys2.rotation_period,sys2.temp_avg,
                        sys1,current_star);
	  writeln(f,'  -Maximum day increase     : +',day_temp,' °C');
	  writeln(f,'  -Maximum night decrease   : -',night_temp,' °C');
          equator:=round((sys2.axial_tilt*0.3+(sys2.diametre/3000+3)*1.5)/2);
          if (sys2.world_type<>3) and (sys2.world_type<>20) then
              writeln(f,'  -Equator increase         : +',equator,' °C');
          polar:=round((sys2.axial_tilt+(sys2.diametre/3000+3)*6)/2);
          if (sys2.world_type<>3) and (sys2.world_type<>20) then
              writeln(f,'  -Polar decrease           : -',polar,' °C');
	  writeln(f,'Satellites          : ',sys2.satellites);
          if sys2.satellites>0 then
             begin
               write(f,'   Diameter    Orbit     Type          Gravity  ');
               writeln(f,'Atmos       Mine        Temp   Colony');
               write(f,'      Km      Kmx1000                     xG    ');
               writeln(f,'Type                     °C');
             end;
          for c:=1 to sys2.satellites do
                 begin
                    sat_display(f,sys2.moon_id+c-1,filename,cyfile);
                 end;
          unusual_display(f,sys2.unusual);
          if is_set(sys2.unusual[3],3) then
                begin
                 main_alienlog(filename,sys2.alien_id,rpg_output,output_form,f);
                end;
         end;
    if is_set(sys2.misc_charac,4) then
         begin
            for counter1:=0 to (filesize(cyfile)-1) do
               begin
                  seek(cyfile,counter1);
                  read(cyfile,sys7);
                  if (sys7.body_type=1) and (sys7.world_id=pln_id) then
                      break;
                 end;
            col_display(f,sys7,output_form,filename);
         end;
   close(cyfile);
end;

{Output form :  0  text file
                1  print
                2  html file}
procedure main_starlog (filename,outstr:string;id:longint;rpg_output,output_form:byte);
var sys1: star_record;
    sys5:names_record;
    nfile:file of names_record;
    sfile: file of star_record;
    sizefile:integer;
    b,a,error_mess:smallint;
    error_result:boolean;
    f: text;
    sector_name:string;
begin

 file_init(sizefile,filename,error_result);
 if error_result then Exit;
 assign (sfile,concat(filename,'.sun'));
 assign (nfile,concat(filename,'.nam'));
 case output_form of
   0 : begin
         assign (f,outstr);
         hr_str:='------------------------------------------------------------------------------';
         b1:='';
         b2:='';
       end;
   1 : begin
         assignprn(f);
         hr_str:='------------------------------------------------------------------------------';
         b1:='';
         b2:='';
       end;
   2 : begin
         assign (f,outstr);
         hr_str:='<hr>';
         b1:='<b>';
         b2:='</b>';
       end;
 end;
 reset(sfile);
 if id>sizefile then
    begin
      error_mess:=Application.MessageBox('Id higher than the maximum Id number',
         'Error', 0);
      Exit;
    end;
 rewrite(f);
 seek(sfile,id);
 read(sfile,sys1);
 reset(nfile);
 read(nfile,sys5);

 if output_form=2 then
    begin
       writeln(f,'<html>');
       writeln(f,'<head>');
       writeln(f,'<title>',sys1.name,'</title>');
       writeln(f,'</head>');
       writeln(f,'<body bgcolor=#FFFFFF>');
       writeln(f,'<pre>');
    end;

 sector_name:=sys5.name;
 writeln(f,b1,'Sector       : ',b2,sector_name);
 writeln(f,b1,'Coordinates  : ',b2,sys1.posX,'/',sys1.posY,'/',sys1.posZ);
 write(f,b1,'System Id    : ',b2,id);
 if sys1.name<>'' then write(f,b1,' [ ',sys1.name,' ]',b2);
 writeln(f);
 close(nfile);

 writeln(f);
 writeln(f,hr_str);
 writeln(f);
 if sys1.spe_class[1]<16 then
    begin
       case sys1.star_nbr of
         1: writeln(f,'Single system');
         2: writeln(f,'Binary system');
         3: writeln(f,'Trinary system');
       end;

       for b:=2 to sys1.star_nbr do
                writeln(f,'    Companion star orbit radii: ',
                  sys1.companion_dist[b-1]/10:0:1,' A.U');
       writeln(f);
       for b:=1 to sys1.star_nbr do
           begin
               sun_display(f,sys1,b,filename,outstr,output_form);
               for a:=1 to sys1.planet_nbr[b] do
	       begin
                   pln_display(sys1.pln_id[b]+a-1,a,b,filename,outstr,sys1,
                   b,rpg_output,output_form,f);
               end;
               writeln(f);
               writeln(f);
           end;
    end
    else
        write(f,'Astral body : ',class_spec[sys1.spe_class[1]]);
if output_form=2 then
   begin
      writeln(f,'</pre>');
      writeln(f,'</body>');
      writeln(f,'</html>')
   end;
close(f);
close(sfile);

end;

end.