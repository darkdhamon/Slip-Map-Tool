{ ** Colony unit v1.6 **}


unit colony1;

interface

  uses recunit,starunit;

procedure main_colony(var economic_power,total_pop,
    trade_bonus,indep_pop,captive_pop,subject_pop,colony_nbr,captive_world_nbr,
    subj_world,moon_nbr,subj_moon,indep_col:Variant ; outstr:string;
    var cyfile:colony_record_file;var ctfile:contact_record_file;
    var afile:alien_record_file);export


implementation


procedure main_colony(var economic_power,total_pop,
    trade_bonus,indep_pop,captive_pop,subject_pop,colony_nbr,captive_world_nbr,
    subj_world,moon_nbr,subj_moon,indep_col:Variant ; outstr:string;
    var cyfile:colony_record_file;var ctfile:contact_record_file;
    var afile:alien_record_file);
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


end.
