{ Alien Generator Logfile for Windows v1.6 by Aina Rasolomalala (c) 03-2001

  v1.57b: fix a bug in color pattern display
  v1.6  : html output }

unit Alienlog;

interface

procedure main_alienlog (filename:string;id:word;rpg_output,output_form:byte;
   var f:text);export;

implementation

uses starunit,recunit, Forms, rpgconv,procunit,printers;

procedure file_init(var sizefile:integer;filename:string;var error_result:boolean);
var afile: file of alien_record;
    error_mess:smallint;
begin
{$I-}
 error_result:=true;
 assignfile (afile,concat(filename,'.aln'));
 reset(afile);
 sizefile:=filesize(afile);
{$I+}
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('aln file empty or doesn''t exist',
         'Error', 0);
       Exit;
    end
 else closefile(afile);
 error_result:=false;
end;

procedure gravity_pref(sys2:planet_record;var max_g,min_g: single);
var escape,gravity,mass:single;
begin
   gravity_calcul(sys2.density,sys2.diametre,mass,gravity,escape);
   max_g:=gravity+0.25;
   min_g:=gravity-0.25;
   if min_g<0.1 then min_g:=0.1;
end;

procedure main_alienlog (filename:string;id:word;rpg_output,output_form:byte;
   var f:text);
var a,error_mess:smallint;
    sizefile:integer;
    b1,b2,result:string;
    tmp:byte;
    first,ability,error_result:boolean;
    sys2: planet_record;
    sys4: alien_record;
    afile : file of alien_record;
    pfile : file of planet_record;
    min_g,max_g:single;
    min_temp,max_temp:smallint;
begin
 file_init(sizefile,filename,error_result);
 if error_result then Exit;
 if id>sizefile then
    begin
      error_mess:=Application.MessageBox('Id higher than the maximum Id number',
         'Error', 0);
      Exit;
    end;
 assignfile(afile,concat(filename,'.aln'));
 assignfile(pfile,concat(filename,'.pln'));
 reset(afile);
 reset(pfile);
 seek(afile,id);
 read(afile,sys4);
  case output_form of
    0,1: begin
           writeln(f,'------------------------------------------------');
           b1:='';
           b2:='';
         end;
    2  : begin
           writeln(f,'<table border><tr><td bgcolor="#d6d6d6">');
           b1:='<b>';
           b2:='</b></td></tr><tr><td><pre>';
         end;
  end;
  write(f,b1,'Alien sheet:  ');
  if sys4.name<>'' then write(f,'[ ',sys4.name,' ]');
  writeln(f,b2);

  writeln(f);
  writeln(f,'Type                 : ',environment_table[sys4.environment_type]);
  writeln(f,'Appearance           : ',appearance_type[sys4.app_genre]);
  seek(pfile,abs(sys4.pln_id));
  read(pfile,sys2);
  if sys2.sun_id>-1 then gravity_pref(sys2,max_g,min_g)
   else
     begin
        max_g:=sys2.pressure+0.25;
        min_g:=sys2.pressure-0.25;
        if min_g<0.1 then min_g:=0.1;
     end;
  writeln(f,'Gravity preferences  : ',min_g:0:1,'-',max_g:0:1,' g');
  min_temp:=sys2.temp_avg-10;
  max_temp:=sys2.temp_avg+10;
  if is_set(sys4.table_abil[2],4) then max_temp:=max_temp+5;
  if is_set(sys4.table_abil[2],1) then max_temp:=max_temp-5;
  writeln(f,'Temperature pref.    : ',min_temp,' to ',max_temp,' °C');
  writeln(f,'Atmosphere breathed  : ',atmos_breath[sys2.atmos]);
  writeln(f,'Body biology         : ',body_table[sys4.body_type]);
  writeln(f,'Body cover           : ',body_cover[sys4.body_cover_type]);
  write(f,'Body color           : ');
  first:=true;
  for a:=1 to 16 do
      begin
        tmp:=1+trunc((a-1)/8);
        if is_set(sys4.color_genre[tmp],a-(tmp-1)*8) then
	  begin
	    if first then first:=false
	       else write(f,',');
	    write(f,color[a]);
	  end;
      end;
  writeln(f);
  if (is_set(sys4.body_char[2],4)) or (is_set(sys4.body_char[2],5)) then
      begin
         write(f,'Number of colors     : ');
         if is_set(sys4.body_char[2],4) then writeln(f,body_part[12])
            else writeln(f,body_part[13]);
         write(f,'Color pattern        : ');
         if is_set(sys4.body_char[2],6) then writeln(f,body_part[14]);
         if is_set(sys4.body_char[2],7) then writeln(f,body_part[15]);
         if is_set(sys4.body_char[2],8) then writeln(f,body_part[16]);
         if is_set(sys4.body_char[2],3) then writeln(f,body_part[11]);
      end;
  if (sys4.hair_char=1) or (sys4.hair_char=3) then
     write(f,'Hair                 : ',hair_part[sys4.hair_char]) else
      begin
         writeln(f,'Hair                 : ',hair_part[sys4.hair_char]);
         write(f,'Hair color           : ');
         first:=true;
         for a:=1 to 16 do
            begin
               tmp:=1+trunc((a-1)/8);
               if is_set(sys4.hair_color[tmp],a-(tmp-1)*8) then
	          begin
	             if first then first:=false
	                else write(f,',');
                     if a=3 then write(f,color[18])
                        else if a=8 then write(f,color[19])
                             else write(f,color[a]);
	          end;
            end;
      end;
  writeln(f);
  write(f,'Eyes                 : ');
  for a:=1 to 14 do
      begin
         tmp:=1+trunc((a-1)/8);
         if is_set(sys4.eye_char[tmp],a-(tmp-1)*8) then
              write(f,eyes_part[a],' ');
      end;
  writeln(f);
  if not is_set(sys4.eye_char[1],1) then
     begin
       write(f,'Eyes color           : ');
       first:=true;
       for a:=1 to 16 do
           begin
                tmp:=1+trunc((a-1)/8);
                if is_set(sys4.eye_color[tmp],a-(tmp-1)*8) then
	           begin
	                if first then first:=false
	                   else write(f,',');
	                if a=8 then write(f,color[17]) else write(f,color[a]);
	           end;
           end;
        writeln(f);
     end;
  write(f,'Body characteristics : ');
  first:=true;
  for a:=1 to 10 do
      begin
         tmp:=1+trunc((a-1)/8);
         if is_set(sys4.body_char[tmp],a-(tmp-1)*8) then
            begin
               if first then first:=false
	          else write(f,',');
               write(f,body_part[a]);
            end;
      end;
  if first then write(f,'None');
  writeln(f);
  writeln(f,'Diet                 : ',diet_type[sys4.diet_genre]);
  writeln(f,'Sexual reproduction  : ',reproduction_type[sys4.repro_genre]);
  writeln(f,'Reproduction method  : ',repro_methode_type[sys4.repro_meth_genre]);
  for a:=1 to sys4.limbs_number do
      writeln(f,'Limbs pair n° ',a,' : ',limbs_type[sys4.limbs_genre[a]]);
  writeln(f,'Mass : ',sys4.mass :4,' kg');
  writeln(f,'Size : ',sys4.size_creat :4,' cm');
  writeln(f);
  writeln(f,'Attributes:');
  for a:=1 to 6 do writeln(f,'  ',charac[a],sys4.attrib[a]);
  for a:=13 to 14 do writeln(f,'  ',charac[a],sys4.attrib[a]);
  write(f,'  ',charac[7]);
  case sys4.attrib[7] of
       1..5 : result:='None';
       6..9: result:='Very Poor';
       10..13: result:='Poor';
       14..16: result:='Fair';
       17..19: result:='Good';
       20 :    result:='Excellent';
  end;
  writeln(f,result);
  for a:=9 to 11 do writeln(f,'  ',charac[a],sys4.attrib[a]);
  if sys4.attrib[12]<200 then
     writeln(f,'  ',charac[12],sys4.attrib[12]*5,' years')
     else writeln(f,'  ',charac[12],'Immortal');
  writeln(f,'  ',charac[8],sys4.attrib[8]);
  if sys4.attrib[8]>6 then writeln(f,'  ',charac[15],sys4.attrib[15],
     ' centuries');
  writeln(f);
  writeln(f,'Government type   : ',gov[sys4.gov_type]);
  writeln(f,'Religion          : ',religion_genre[sys4.religion]);
  write(f,'Devotion          : ');
  case sys4.devotion of
       0   : result:='None';
       1,2 : result:='Poor';
       3..6: result:='Fair';
       7..9: result:='Good';
       10  : result:='High';
  end;
  writeln(f,result);
  writeln(f);
  writeln(f,'Special abilities:');
  ability:=false;
  for a:=1 to max_abilities do
     begin
          tmp:=1+trunc((a-1)/8);
          if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                           begin
			    writeln(f,'  -',special_ability[a]);
			    ability:=true;
			   end;
     end;
  if not ability then writeln(f,'  -None');
  writeln(f);
  case rpg_output of
    1: alternity_conversion (sys4,output_form,f);
    2: battle_conversion (sys4,output_form,f);
    3: gurps_conversion (sys4,output_form,f);
    4: master_conversion (sys4,output_form,f);
    5: fuzion_conversion (sys4,output_form,f);
  end;
  case output_form of
    0,1: writeln(f,'------------------------------------------------');
    2  : writeln(f,'</pre></td></tr></table>');
  end;
  writeln(f);
  close(afile);
  close(pfile);
 end;
end.