{ *** Procedure to update old Star Generator datas  v1.6 ***
      v1.56 -> v1.6 }

unit update;

interface


procedure update_data(namefile:string;old_ver:byte);export;

implementation

uses SysUtils,recunit,procunit;

TYPE   table_bool2_157a=array[1..11] of byte;

       colony_rec_156  = record
                          world_id: longint;
                          race,allegiance:word;
                          body_type,pop,col_class,crime,law,stability,
                          age,starport,gov: byte;
                          gnp,power: word;
                         end;
       colony_rec_157 = record
                          world_id: longint;
                          race,allegiance:word;
                          body_type,pop,col_class,crime,law,stability,
                          age,starport,gov: byte;
                          gnp,power: word;
                          pop_comp: byte;
                          misc_char: byte;
                         end;
        alien_rec_157a = record
                          pln_id: longint;
                          environment_type,body_type,limbs_number,diet_genre,
                          repro_genre,repro_meth_genre,gov_type,body_cover_type,
                          app_genre:byte;
                          mass,size_creat:smallint;
                          limbs_genre:table_byte;
                          attrib:table_byte2;
                          table_abil:table_bool2_157a;
                          color_genre,hair_color,body_char,eye_color,eye_char:table_bool3;
                          hair_char,religion,devotion:byte;
                         end;
        alien_rec_157c= record
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
                        end;
        star_rec_157c = record
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
                         end;
        planet_rec_157c= record
                           sun_id: longint;
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
                           misc_charac: byte;
                           unusual: table_bool;
                         end;
        moon_rec_157c=   record
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
                         end;



procedure update_data(namefile:string;old_ver:byte);
var a:longint;
    tmp:byte;
    sys1:star_record;
    osys1:star_rec_157c;
    sys2:planet_record;
    osys3:moon_rec_157c;
    sys3:moon_record;
    osys2:planet_rec_157c;
    o1sys4:alien_rec_157c;
    osys4: alien_rec_157a;
    sys4: alien_record;
    sys5: names_record;
    sys7: colony_record;
    osys7: colony_rec_156;
    o1sys7: colony_rec_157;
    nfile: file of names_record;
    pfile: file of planet_record;
    opfile: file of planet_rec_157c;
    o1afile: file of alien_rec_157c;
    oafile: file of alien_rec_157a;
    afile: file of alien_record;
    mfile: file of moon_record;
    omfile: file of moon_rec_157c;
    ctfile: file of contact_record;
    ocyfile: file of colony_rec_156;
    o1cyfile: file of colony_rec_157;
    cyfile: file of colony_record;
    efile: file of empire_record;
    sfile: file of star_record;
    osfile: file of star_rec_157c;
    error_value:boolean;
begin
    assignfile (efile,concat(namefile,'.emp'));
    assignfile (ctfile,concat(namefile,'.con'));
    assignfile (cyfile,concat(namefile,'.col'));
    assignfile (sfile,concat(namefile,'.sun'));
    assignfile (pfile,concat(namefile,'.pln'));
    assignfile (mfile,concat(namefile,'.mon'));

    if old_ver<5 then {Updating colony record to v1.57a}
      begin
        if (FileExists (concat(namefile,'.col'))) and (old_ver<5) then
          begin
            error_value:=RenameFile(concat(namefile,'.col'),concat(namefile,'.col2'));
            assignfile (ocyfile,concat(namefile,'.col2'));
            reset (ocyfile);
            rewrite (cyfile);
            if filesize (ocyfile)>0 then
              begin
                for a:=0 to (filesize (ocyfile)-1) do
                 begin
                  read(ocyfile,osys7);
                  sys7.world_id:=osys7.world_id;
                  sys7.race:=osys7.race;
                  sys7.allegiance:=osys7.allegiance;
                  sys7.body_type:=osys7.body_type;
                  sys7.pop:=osys7.pop;
                  sys7.col_class:=osys7.col_class;
                  sys7.crime:=osys7.crime;
                  sys7.law:=osys7.law;
                  sys7.stability:=osys7.stability;
                  sys7.age:=osys7.age;
                  sys7.starport:=osys7.starport;
                  sys7.gov:=osys7.gov;
                  sys7.gnp:=osys7.gnp;
                  sys7.power:=osys7.power;
                  sys7.pop_comp:=100;
                  sys7.misc_char[1]:=0;
                  sys7.misc_char[2]:=0;
                  write(cyfile,sys7);
                 end;
              end;
            closefile(cyfile);
            closefile(ocyfile);
            error_value:=deletefile(concat(namefile,'.col2'));
          end;
      end;

    if (old_ver>4) and (old_ver<8) then {Updating colony v1.57a=> v1.6}
      begin
        if FileExists (concat(namefile,'.col')) then
          begin
            error_value:=RenameFile(concat(namefile,'.col'),concat(namefile,'.col2'));
            assignfile (o1cyfile,concat(namefile,'.col2'));
            reset (o1cyfile);
            rewrite (cyfile);
            if filesize (o1cyfile)>0 then
              begin
                for a:=0 to (filesize (o1cyfile)-1) do
                 begin
                  read(o1cyfile,o1sys7);
                  sys7.world_id:=o1sys7.world_id;
                  sys7.race:=o1sys7.race;
                  sys7.allegiance:=o1sys7.allegiance;
                  sys7.body_type:=o1sys7.body_type;
                  sys7.pop:=o1sys7.pop;
                  sys7.col_class:=o1sys7.col_class;
                  sys7.crime:=o1sys7.crime;
                  sys7.law:=o1sys7.law;
                  sys7.stability:=o1sys7.stability;
                  sys7.age:=o1sys7.age;
                  sys7.starport:=o1sys7.starport;
                  sys7.gov:=o1sys7.gov;
                  sys7.gnp:=o1sys7.gnp;
                  sys7.power:=o1sys7.power;
                  sys7.pop_comp:=o1sys7.pop_comp;
                  sys7.misc_char[1]:=o1sys7.misc_char;
                  sys7.misc_char[2]:=0;
                  write(cyfile,sys7);
                 end;
              end;
            closefile(cyfile);
            closefile(o1cyfile);
            error_value:=deletefile(concat(namefile,'.col2'));
          end;
      end;

    assignfile (opfile,concat(namefile,'.pln'));
    reset(opfile);
    if old_ver<3 then
      for a:=0 to (filesize(opfile)-1) do
         begin
            seek(opfile,a);
            read(opfile,osys2);
            if old_ver=1 then osys2.allegiance:=65535;

            {Updating v1.56 -> v1.56b}
            if old_ver<3 then
             case osys2.world_type of
              15,16,17,18: begin
                            osys2.atmos_type:=6;
                            osys2.atmos:=1;
                            osys2.satellites:=0;
                            if osys2.world_type=18 then
                               begin
                                   osys2.water_type:=3;
                                   osys2.hydrography[2]:=100;
                                   osys2.hydrography[4]:=35;
                               end
                               else
                               begin
                                   osys2.water_type:=1;
                                   osys2.hydrography[4]:=15-5*(osys2.world_type-15);
                               end;
                           end;
             end;
            seek(opfile,a);
            write(opfile,osys2);
         end;
    closefile (opfile);

    case old_ver of
      1:begin
          rewrite (ctfile);
          rewrite (cyfile);
          closefile (ctfile);
          closefile (cyfile);
        end;
    end;
    if old_ver<4 then
       begin
         rewrite(efile);
         closefile(efile);
       end;
    if old_ver<6 then {Updating alien record to v1.57b}
      begin
        if (FileExists (concat(namefile,'.aln'))) and (old_ver<6) then
          begin
            error_value:=RenameFile(concat(namefile,'.aln'),concat(namefile,'.aln2'));
            assignfile (oafile,concat(namefile,'.aln2'));
            assignfile (o1afile,concat(namefile,'.aln'));
            reset (oafile);
            rewrite (o1afile);
            if filesize (oafile)>0 then
              begin
                for a:=0 to (filesize (oafile)-1) do
                 begin
                  read(oafile,osys4);
                  o1sys4.pln_id:=osys4.pln_id;
                  o1sys4.environment_type:=osys4.environment_type;
                  o1sys4.body_type:=osys4.body_type;
                  o1sys4.limbs_number:=osys4.limbs_number;
                  o1sys4.diet_genre:=osys4.diet_genre;
                  o1sys4.repro_genre:=osys4.repro_genre;
                  o1sys4.repro_meth_genre:=osys4.repro_meth_genre;
                  o1sys4.gov_type:=osys4.gov_type;
                  o1sys4.body_cover_type:=osys4.body_cover_type;
                  o1sys4.app_genre:=osys4.app_genre;
                  o1sys4.mass:=osys4.mass;
                  o1sys4.size_creat:=osys4.size_creat;
                  o1sys4.limbs_genre:=osys4.limbs_genre;
                  o1sys4.attrib:=osys4.attrib;
                  o1sys4.color_genre:=osys4.color_genre;
                  o1sys4.hair_color:=osys4.hair_color;
                  o1sys4.body_char:=osys4.body_char;
                  o1sys4.eye_color:=osys4.eye_color;
                  o1sys4.eye_char:=osys4.eye_char;
                  o1sys4.hair_char:=osys4.hair_char;
                  o1sys4.religion:=osys4.religion;
                  o1sys4.devotion:=osys4.devotion;
                  for tmp:=1 to 11 do
                      o1sys4.table_abil[tmp]:=osys4.table_abil[tmp];
                  o1sys4.table_abil[12]:=0;
                  if is_set(o1sys4.table_abil[3],6) then     {No more solar sustenance}
                     begin
                       unset_bit(o1sys4.table_abil[3],6);
                       o1sys4.diet_genre:=6;
                     end;
                  write(o1afile,o1sys4);
                 end;
              end;
          end;
          closefile(o1afile);
          closefile(oafile);
          error_value:=deletefile(concat(namefile,'.aln2'));
       end;

    {******** v1.6 update the name slot ************}
    if old_ver<8 then
       begin
          error_value:=RenameFile(concat(namefile,'.sun'),concat(namefile,'.sun2'));
          assignfile (osfile,concat(namefile,'.sun2'));
          reset (osfile);
          rewrite (sfile);
          for a:=0 to (filesize(osfile)-1) do
             begin
                seek(osfile,a);
                read(osfile,osys1);
                sys1.spe_class:=osys1.spe_class;
                sys1.type_spec:=osys1.type_spec;
                sys1.planet_nbr:=osys1.planet_nbr;
                sys1.luminosity:=osys1.luminosity;
                sys1.mass_star:=osys1.mass_star;
                sys1.age:=osys1.age;
                sys1.companion_dist:=osys1.companion_dist;
                sys1.astral_orbit:=osys1.astral_orbit;
                sys1.star_nbr:=osys1.star_nbr;
                sys1.pln_id:=osys1.pln_id;
                sys1.posX:=osys1.posX;
                sys1.posY:=osys1.posY;
                sys1.posZ:=osys1.posZ;
                sys1.allegiance:=osys1.allegiance;
                sys1.code:=osys1.code;
                sys1.name:='';
                sys1.misc_char:=0;
                seek(sfile,a);
                write(sfile,sys1);
             end;
          closefile(sfile);
          closefile(osfile);
          error_value:=deletefile(concat(namefile,'.sun2'));

          error_value:=RenameFile(concat(namefile,'.pln'),concat(namefile,'.pln2'));
          assignfile (opfile,concat(namefile,'.pln2'));
          reset (opfile);
          rewrite (pfile);
          for a:=0 to (filesize(opfile)-1) do
             begin
                seek(opfile,a);
                read(opfile,osys2);
                sys2.sun_id:=osys2.sun_id;
                sys2.alien_id:=osys2.alien_id;
                sys2.moon_id:=osys2.moon_id;
                sys2.allegiance:=osys2.allegiance;
                sys2.atmos_type:=osys2.atmos_type;
                sys2.world_type:=osys2.world_type;
                sys2.water_type:=osys2.water_type;
                sys2.atmos:=osys2.atmos;
                sys2.smwr:=osys2.smwr;
                sys2.axial_tilt:=osys2.axial_tilt;
                sys2.inclination:=osys2.inclination;
                sys2.temp_avg:=osys2.temp_avg;
                sys2.satellites:=osys2.satellites;
                sys2.hydrography:=osys2.hydrography;
                sys2.rotation_period:=osys2.rotation_period;
                sys2.diametre:=osys2.diametre;
                sys2.density:=osys2.density;
                sys2.pressure:=osys2.pressure;
                sys2.eccentricity:=osys2.eccentricity;
                sys2.orbit_radius:=osys2.orbit_radius;
                sys2.mine_ress:=osys2.mine_ress;
                sys2.misc_charac:=osys2.misc_charac;
                sys2.unusual:=osys2.unusual;
                sys2.name:='';
                seek(pfile,a);
                write(pfile,sys2);
             end;
          closefile(pfile);
          closefile(opfile);
          error_value:=deletefile(concat(namefile,'.pln2'));

          error_value:=RenameFile(concat(namefile,'.mon'),concat(namefile,'.mon2'));
          assignfile (omfile,concat(namefile,'.mon2'));
          reset (omfile);
          rewrite (mfile);
          for a:=0 to (filesize(omfile)-1) do
             begin
                seek(omfile,a);
                if filesize(omfile)>0 then
                   begin
                     read(omfile,osys3);
                     sys3.pln_id:=osys3.pln_id;
                     sys3.atmos_type:=osys3.atmos_type;
                     sys3.world_type:=osys3.world_type;
                     sys3.water_type:=osys3.water_type;
                     sys3.atmos:=osys3.atmos;
                     sys3.temp_avg:=osys3.temp_avg;
                     sys3.hydrography:=osys3.hydrography;
                     sys3.diametre:=osys3.diametre;
                     sys3.density:=osys3.density;
                     sys3.pressure:=osys3.pressure;
                     sys3.sat_orbit:=osys3.sat_orbit;
                     sys3.mine_ress:=osys3.mine_ress;
                     sys3.misc_charac:=osys3.misc_charac;
                     sys3.name:='';
                     seek(mfile,a);
                     write(mfile,sys3);
                   end;
             end;
          closefile(mfile);
          closefile(omfile);
          error_value:=deletefile(concat(namefile,'.mon2'));

          error_value:=RenameFile(concat(namefile,'.aln'),concat(namefile,'.aln2'));
          assignfile (afile,concat(namefile,'.aln'));
          assignfile (o1afile,concat(namefile,'.aln2'));
          reset (o1afile);
          rewrite (afile);
          for a:=0 to (filesize(o1afile)-1) do
             begin
                seek(o1afile,a);
                if filesize(o1afile)>0 then
                   begin
                     read(o1afile,o1sys4);
                     sys4.pln_id:=o1sys4.pln_id;
                     sys4.environment_type:=o1sys4.environment_type;
                     sys4.body_type:=o1sys4.body_type;
                     sys4.limbs_number:=o1sys4.limbs_number;
                     sys4.diet_genre:=o1sys4.diet_genre;
                     sys4.repro_genre:=o1sys4.repro_genre;
                     sys4.repro_meth_genre:=o1sys4.repro_meth_genre;
                     sys4.gov_type:=o1sys4.gov_type;
                     sys4.body_cover_type:=o1sys4.body_cover_type;
                     sys4.app_genre:=o1sys4.app_genre;
                     sys4.mass:=o1sys4.mass;
                     sys4.size_creat:=o1sys4.size_creat;
                     sys4.limbs_genre:=o1sys4.limbs_genre;
                     sys4.attrib:=o1sys4.attrib;
                     if (sys4.app_genre=18) and (sys4.attrib[8]<6) then
                        begin
                           sys4.attrib[8]:=6;
                           sys4.attrib[15]:=de(5,1);
                        end;
                     sys4.table_abil:=o1sys4.table_abil;
                     sys4.color_genre:=o1sys4.color_genre;
                     sys4.hair_color:=o1sys4.hair_color;
                     sys4.body_char:=o1sys4.body_char;
                     sys4.eye_color:=o1sys4.eye_color;
                     sys4.hair_char:=o1sys4.hair_char;
                     sys4.religion:=o1sys4.religion;
                     sys4.devotion:=o1sys4.devotion;
                     sys4.name:='';
                     seek(afile,a);
                     write(afile,sys4);
                   end;
             end;
          closefile(afile);
          closefile(o1afile);
          error_value:=deletefile(concat(namefile,'.aln2'));

          {Updating names}
          assignfile (afile,concat(namefile,'.aln'));
          assignfile (sfile,concat(namefile,'.sun'));
          assignfile (pfile,concat(namefile,'.pln'));
          assignfile (mfile,concat(namefile,'.mon'));
          assignfile (nfile,concat(namefile,'.nam'));
          reset(nfile);
          reset(pfile);
          reset(afile);
          reset(mfile);
          reset(sfile);
          for a:= 0 to (filesize(nfile)-1) do
             begin
               read(nfile,sys5);
               if a>0 then
                  begin
                    case sys5.body_type of
                       1: begin
                            seek(pfile,sys5.body_Id);
                            read(pfile,sys2);
                            sys2.name:=sys5.name;
                            seek(pfile,sys5.body_Id);
                            write(pfile,sys2);
                          end;
                       2: begin
                            seek(mfile,sys5.body_Id);
                            read(mfile,sys3);
                            sys3.name:=sys5.name;
                            seek(mfile,sys5.body_Id);
                            write(mfile,sys3);
                          end;
                       3: begin
                            seek(sfile,sys5.body_Id);
                            read(sfile,sys1);
                            sys1.name:=sys5.name;
                            seek(sfile,sys5.body_Id);
                            write(sfile,sys1);
                          end;
                       4: begin
                            seek(afile,sys5.body_Id);
                            read(afile,sys4);
                            sys4.name:=sys5.name;
                            seek(afile,sys5.body_Id);
                            write(afile,sys4);
                          end;
                    end;
                  end;
             end;
          seek(nfile,0);
          read(nfile,sys5);
          closefile(afile);
          closefile(sfile);
          closefile(pfile);
          closefile(mfile);
          closefile(nfile);
          error_value:=deletefile(concat(namefile,'.nam'));
          rewrite(nfile);
          write(nfile,sys5);
          closefile(nfile);
       end;
    {******** v1.6 end of the name slot update ************}

end;


end.
