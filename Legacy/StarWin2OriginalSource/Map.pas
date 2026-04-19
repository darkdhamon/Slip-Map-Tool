{ Sector Mapper for Windows v1.6 by Aina Rasolomalala (c) 02-2001

   v1.2  Use now *.emp files
 }

unit map;

interface

procedure main_map(filename,outstr:string;system,alien,summary,empire:boolean);export;

implementation

uses recunit,starunit,Forms,Controls,colony1;

type count_obj=array[1..26] of integer;
     stat_obj=array[1..15,1..3] of longint;

const max_races=26; {Max number of alien appearances}

procedure file_init(outstr:string; var error_result:boolean);
var pfile: file of planet_record;
    sfile: file of star_record;
    mfile: file of moon_record;
    error_mess: smallint;
begin
{$I-}
 error_result:=true;
 AssignFile (pfile,concat(outstr,'.pln'));
 AssignFile (sfile,concat(outstr,'.sun'));
 AssignFile (mfile,concat(outstr,'.mon'));
 reset(sfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('sun file empty or doesn''t exist',
         'Error', 0);
       close(sfile);
       Exit;
    end;
 reset(pfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('pln file doesn''t exist','Error',0);
       close(pfile);
       Exit;
    end;
 reset(mfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('mon file doesn''t exist','Error',0);
       close(mfile);
       Exit;
    end;
 close(pfile);
 close(sfile);
 close(mfile);
 error_result:=false;
{$I+}
end;


procedure main_map(filename,outstr:string;system,alien,summary,empire:boolean);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    sys5: names_record;
    sys8: contact_record;
    sys9: empire_record;
    sfile: file of star_record;
    pfile: file of planet_record;
    mfile: file of moon_record;
    afile: file of alien_record;
    nfile: file of names_record;
    ctfile: file of contact_record;
    cyfile: file of colony_record;
    efile: file of empire_record;
    compteur,c:longint;
    star_count,planet_count,moon_count,alien_count:count_obj;
    f:text;
    life,error_result,flag:boolean;
    a,b:byte;
    mess:smallint;
    system_sizefile,sun_sizefile,moon_sizefile,planet_sizefile:integer;
    alien_sizefile:integer;
    sector_name:string;
    prestige:longint;
    max_stat,best_stat:stat_obj;
begin
    file_init(filename,error_result);
    if error_result then Exit;
    Screen.Cursor:=crHourglass;
    assignfile (sfile,concat(filename,'.sun'));
    assignfile (pfile,concat(filename,'.pln'));
    assignfile (mfile,concat(filename,'.mon'));
    assignfile (afile,concat(filename,'.aln'));
    assignfile (nfile,concat(filename,'.nam'));
    assignfile (ctfile,concat(filename,'.con'));
    assignfile (cyfile,concat(filename,'.col'));
    assignfile (f,concat(outstr,'.map'));
    assignfile (efile,concat(filename,'.emp'));

    reset(nfile);
    read(nfile,sys5);
    sector_name:=sys5.name;
    close(nfile);

    for a:=1 to 26 do
        begin
           star_count[a]:=0;
           planet_count[a]:=0;
           moon_count[a]:=0;
           alien_count[a]:=0;
        end;
    reset(sfile);
    reset(pfile);
    reset(mfile);
    reset(afile);
    system_sizefile:=filesize(sfile);
    sun_sizefile:=0;
    moon_sizefile:=filesize(mfile);
    planet_sizefile:=filesize(pfile);
    alien_sizefile:=filesize(afile);
    compteur:=0;

    rewrite(f);
    writeln(f,' Sector : ',sector_name);
    writeln(f,' -------');
    writeln(f);
    if system then
      begin
        writeln(f);
        writeln(f);
        writeln(f,'    Id     X     Y     Z   Life  Star(s)');
        writeln(f,'--------------------------------------------------------');
      repeat
          life:=false;
          seek(sfile,compteur);
          read(sfile,sys1);
          write(f,compteur:6,' ');
          write(f,sys1.posX:5,' ');
          write(f,sys1.posY:5,' ');
          write(f,sys1.posZ:5,'    ');
          for a:=1 to sys1.star_nbr do
              begin
                 sun_sizefile:=sun_sizefile+1;
                 for b:=1 to sys1.planet_nbr[a] do
                  begin
                     seek(pfile,sys1.pln_id[a]+b-1);
                     read(pfile,sys2);
                     planet_count[sys2.world_type]:=planet_count[sys2.world_type]+1;
                     if is_set(sys2.unusual[3],3) then life:=true;
                  end;
              end;
           if life then write(f,'Y    ') else write(f,'     ');;
          for a:=1 to sys1.star_nbr do
              begin
                write(f,class_spec[sys1.spe_class[a]]);
                if sys1.spe_class[a]<15 then write(f,sys1.type_spec[a]);
                case sys1.spe_class[a] of
                  1,11: write(f,' II');
                  2   : write(f,' III');
                  3   : write(f,' IV');
                  4..7: write(f,' V');
                  8   : write(f,' VI');
                  9   : write(f,' VII');
                  12,13: write(f,' Ia');
                  10  : write(f,' Ib');
                end;
                if a<sys1.star_nbr then write(f,',');
                star_count[sys1.spe_class[a]]:=star_count[sys1.spe_class[a]]+1;
              end;
          writeln(f);
          compteur:=compteur+1;
        until compteur=system_sizefile;
        for compteur:=0 to (moon_sizefile-1) do
           begin
             seek(mfile,compteur);
             read(mfile,sys3);
             moon_count[sys3.world_type]:=moon_count[sys3.world_type]+1;
           end;
        writeln(f);
        writeln(f);
    end
    else
    begin
      repeat
          seek(sfile,compteur);
          read(sfile,sys1);
          for a:=1 to sys1.star_nbr do
              begin
                 sun_sizefile:=sun_sizefile+1;
                 star_count[sys1.spe_class[a]]:=star_count[sys1.spe_class[a]]+1;
                 for b:=1 to sys1.planet_nbr[a] do
                  begin
                     seek(pfile,sys1.pln_id[a]+b-1);
                     read(pfile,sys2);
                     planet_count[sys2.world_type]:=planet_count[sys2.world_type]+1;
                  end;
              end;
          compteur:=compteur+1;
        until compteur=system_sizefile;
        for compteur:=0 to (moon_sizefile-1) do
           begin
             seek(mfile,compteur);
             read(mfile,sys3);
             moon_count[sys3.world_type]:=moon_count[sys3.world_type]+1;
           end;
    end;
    if alien then
      begin
        writeln(f);
        writeln(f,'Alien Races');
        writeln(f,'***********');
        writeln(f);
        writeln(f,'   Id SysId   Appearance    TL  Government');
        writeln(f,'--------------------------------------------------------');
        for compteur:=0 to (alien_sizefile-1) do
          begin
            seek(afile,compteur);
            read(afile,sys4);
            alien_count[sys4.app_genre]:=alien_count[sys4.app_genre]+1;
            seek(pfile,abs(sys4.pln_id));
            read(pfile,sys2);
            write(f,compteur:5,sys2.sun_id:6,'  ',appearance_type[sys4.app_genre]);
            write(f,' ',sys4.attrib[8]:2);
            writeln(f,'   ',gov[sys4.gov_type]);
          end;
        writeln(f);
      end
      else
      begin
        for compteur:=0 to (alien_sizefile-1) do
          begin
            seek(afile,compteur);
            read(afile,sys4);
            alien_count[sys4.app_genre]:=alien_count[sys4.app_genre]+1;
          end;
      end;
    close(sfile);
    close(pfile);
    close(mfile);
    close(afile);

    if summary then
      begin
        writeln(f);
        writeln(f);
        writeln(f,'------------------------------------------------------------------------------');
        writeln(f);
        writeln(f,'Sector summary');
        writeln(f,'**************');
        writeln(f);
        writeln(f);
        writeln(f,'Stellar systems: ',system_sizefile);
        writeln(f,'----------------');
        writeln(f);
        writeln(f,'Stars: ',sun_sizefile);
        writeln(f,'------');
        for a:=1 to 22 do
         begin
          if star_count[a]>0 then
             begin
                write(f,class_spec[a]);
                case a of
                  1,11: write(f,' II      :');
                  2   : write(f,' III     :');
                  3   : write(f,' IV      :');
                  4..7: write(f,' V       :');
                  8   : write(f,' VI      :');
                  9   : write(f,' VII     :');
                  12,13: write(f,' Ia      :');
                  10  : write(f,' Ib      :');
                  15  : write(f,'    :');
                  16,18: write(f,'    :');
                  17,19,22: write(f,':');
                end;
                writeln(f,' ',star_count[a]:6);
             end;
         end;
        writeln(f);
        writeln(f);
        writeln(f);
        writeln(f,'Planets: ',planet_sizefile);
        writeln(f,'--------');
        for a:=1 to 23 do
           begin
             if planet_count[a]>0 then writeln(f,world_genre[a],':',
                planet_count[a]:6);
           end;
        writeln(f);
        writeln(f);
        writeln(f,'Satellites: ',moon_sizefile);
        writeln(f,'-----------');
        for a:=1 to 23 do
            begin
              if moon_count[a]>0 then writeln(f,world_genre[a],':',
                 moon_count[a]:6);
            end;
        writeln(f);
        writeln(f);
        writeln(f,'Alien Races: ',alien_sizefile);
        writeln(f,'------------');
        for a:=1 to max_races do
            begin
              if alien_count[a]>0 then writeln(f,appearance_type[a],':',
                 alien_count[a]:6);
            end;
        writeln(f);
      end;
    reset(cyfile);
    if empire and (filesize(cyfile)>0) then
      begin
        writeln(f);
        writeln(f,'------------------------------------------------------------------------------');
        writeln(f);
        writeln(f,'Empires Infos');
        writeln(f,'*************');
        writeln(f);
        reset(ctfile);
        reset(efile);
        for a:=1 to 3 do
            for compteur:=1 to 15 do
               begin
                  best_stat[compteur,a]:=-1;
                  max_stat[compteur,a]:=0;
               end;
        for compteur:=0 to (alien_sizefile-1) do
          begin
            seek(efile,compteur);
            read(efile,sys9);
            writeln(f,'Race: ',compteur);
            writeln(f,'Military power    : ',sys9.attrib[2]);
            writeln(f,'Economic power    : ',sys9.attrib[1],' MCr');
            if sys9.attrib[4]>0 then
               writeln(f,'Trade bonus       : ',sys9.attrib[4],' MCr');
            if sys9.attrib[8]>0 then
               writeln(f,'Worlds            : ',sys9.attrib[8]);
            if sys9.attrib[9]>0 then
               writeln(f,'Captive Worlds    : ',sys9.attrib[9]);
            if sys9.attrib[10]>0 then
               writeln(f,'Subjugated worlds : ',sys9.attrib[10]);
            if sys9.attrib[11]>0 then writeln(f,'Moons             : ',sys9.attrib[11]);
            if sys9.attrib[12]>0 then writeln(f,'Subjugated moons  : ',sys9.attrib[12]);
            if sys9.attrib[13]>0 then writeln(f,'Independant col.  : ',sys9.attrib[13]);
            if sys9.attrib[3]>0 then
               writeln(f,'Population        : ',sys9.attrib[3],' millions');
            if sys9.attrib[5]>0 then
               writeln(f,'Captive Pop.      : ',sys9.attrib[5],' millions');
            if sys9.attrib[7]>0 then
               writeln(f,'Subjects Pop.     : ',sys9.attrib[7],' millions');
            if sys9.attrib[6]>0 then
               writeln(f,'Independant Pop.  : ',sys9.attrib[6],' millions');
            prestige:=trunc(((sys9.attrib[3]+sys9.attrib[7])/5000+(sys9.attrib[8]+
               sys9.attrib[10])/50+sys9.attrib[2]/25000+sys9.attrib[1]/50000)/4);
            for a:=1 to 10 do
               begin
                 if (prestige>max_stat[a,1]) then
                    begin
                      if a<10 then
                        begin
                          for c:=10 downto (a+1) do
                             begin
                               best_stat[c,1]:=best_stat[c-1,1];
                               max_stat[c,1]:=max_stat[c-1,1];
                             end;
                             best_stat[a,1]:=compteur;
                             max_stat[a,1]:=prestige;
                             break;
                        end;
                    end;
               end;
            for a:=1 to 10 do
               begin
                 if (sys9.attrib[2]>max_stat[a,2]) then
                    begin
                      if a<10 then
                        begin
                          for c:=10 downto (a+1) do
                             begin
                               best_stat[c,2]:=best_stat[c-1,2];
                               max_stat[c,2]:=max_stat[c-1,2];
                             end;
                             best_stat[a,2]:=compteur;
                             max_stat[a,2]:=sys9.attrib[2];
                             break;
                        end;
                    end;
               end;
            for a:=1 to 15 do
               begin
                 if ((sys9.attrib[8]+sys9.attrib[10])>max_stat[a,3]) then
                    begin
                      if a<15 then
                        begin
                          for c:=15 downto (a+1) do
                             begin
                               best_stat[c,3]:=best_stat[c-1,3];
                               max_stat[c,3]:=max_stat[c-1,3];
                             end;
                             best_stat[a,3]:=compteur;
                             max_stat[a,3]:=sys9.attrib[8]+sys9.attrib[10];
                             break;
                        end;
                    end;
               end;
            writeln(f,'Prestige          : ',prestige);
            writeln(f,'Relations         : ');
            seek(ctfile,0);
            flag:=false;
            for c:=0 to (filesize(ctfile)-1) do
              begin
               read(ctfile,sys8);
               if sys8.empire1=compteur then
                    begin
                      writeln(f,'  ',sys8.empire2:4,' ',relation_genre[sys8.relation]);
                      flag:=true;
                    end;
               if sys8.empire2=compteur then
                    begin
                      writeln(f,'  ',sys8.empire1:4,' ',relation_genre[sys8.relation]);
                      flag:=true;
                    end;
              end;
            if not flag then writeln(f,'   None');
            writeln(f);
        end;
        closefile(efile);
        closefile(ctfile);
        writeln(f);
        writeln(f,'------------------------------------------------------------------------------');
        writeln(f);
        writeln(f,'Empire stats');
        writeln(f,'************');
        writeln(f);
        writeln(f,'Most Prestigious Empires:');
        for a:=1 to 10 do
           begin
             if max_stat[a,1]>0 then writeln(f,'  ',a:2,' Race ',best_stat[a,1]:4,'  ',
                max_stat[a,1]);
           end;
        writeln(f);
        writeln(f,'Most Powerful Empires:');
        for a:=1 to 10 do
           begin
             if best_stat[a,2]>0 then writeln(f,'  ',a:2,' Race ',best_stat[a,2]:4,'  ',
                max_stat[a,2]);
           end; 
        writeln(f);
        writeln(f,'Most Important Empires: (in worlds)');
        for a:=1 to 15 do
           begin
             if best_stat[a,3]>1 then writeln(f,'  ',a:2,' Race ',best_stat[a,3]:4,'  ',
                max_stat[a,3]);
           end;
      end;
    closefile(cyfile);
    closefile(f);
    Screen.Cursor:=crdefault;
    mess:=Application.MessageBox('Sector Report Done','Info',0);

end;


end.
