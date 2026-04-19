unit colony_rept;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls, Buttons, ExtCtrls;

type
  TForm8 = class(TForm)
    Bevel1: TBevel;
    Label1: TLabel;
    Edit1: TEdit;
    Label3: TLabel;
    Edit3: TEdit;
    Label2: TLabel;
    Edit2: TEdit;
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    OpenDialog1: TOpenDialog;
    SaveDialog1: TSaveDialog;
    procedure Edit1Click(Sender: TObject);
    procedure Edit3Click(Sender: TObject);
    procedure OKBtnClick(Sender: TObject);
  private
    { Déclarations privées }
  public
    { Déclarations publiques }
  end;

var
  Form8: TForm8;

implementation

{$R *.DFM}
uses starunit,recunit;

var namefile,outfile:string;


procedure main_viewer;
var sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    sys7: colony_record;
    sys8: contact_record;
    sys1: star_record;
    pfile: file of planet_record;
    mfile: file of moon_record;
    cyfile: file of colony_record;
    ctfile: file of contact_record;
    afile: file of alien_record;
    sfile: file of star_record;
    a: longint;
    facilities: boolean;
    military_power,total_pop,trade_bonus,subj_pop,subject_pop:longint;
    colony_nbr,captive_world_nbr,subj_world,moon_nbr,subj_moon:word;
    multiplier_pop:single;
    factor_pop,colony_type,prestige:byte;
    f:text;
    econ_value: ARRAY [1..2] of longint;
    raceview:longint;
    b,tmp:byte;
    code,system_count:integer;
    sysowner: array [0..65535] of integer;
const pop_index: ARRAY [1..7] of single=
   (0.01,0.1,1,10,100,1000,10000);
begin
 assignfile (cyfile,concat(namefile,'.col'));
 assignfile (ctfile,concat(namefile,'.con'));
 assignfile (pfile,concat(namefile,'.pln'));
 assignfile (mfile,concat(namefile,'.mon'));
 assignfile (sfile,concat(namefile,'.sun'));
 assignfile (f,concat(outfile,'.log'));
 assignfile (afile,concat(namefile,'.aln'));
 rewrite(f);
 reset(pfile);
 reset(mfile);
 reset(cyfile);
 reset(ctfile);
 reset(afile);
 reset(sfile);
 for a:=0 to 65535 do sysowner[a]:=-1;
 system_count:=0;
 val(Form8.Edit2.Text,raceview,code);
 writeln(f,'   Colonies owned by Race [',raceview:6,' ]');
 writeln(f,'   --------------------------------');
 writeln(f);
 writeln(f);
 for a:=0 to (filesize(cyfile)-1) do
    begin
       read(cyfile,sys7);
      if (sys7.allegiance=raceview) or ((sys7.race=raceview) and (sys7.allegiance=65535)) then
      begin
       if sys7.body_type=1 then
          begin
            seek(pfile,sys7.world_id);
            read(pfile,sys2);
            colony_type:=sys2.world_type;
          end
        else
          begin
            seek(mfile,sys7.world_id);
            read(mfile,sys3);
            colony_type:=sys3.world_type;
            seek(pfile,sys3.pln_id);
            read(pfile,sys2);
          end;
       if sys7.body_type=1 then writeln(f,'Colony: ',a)
          else writeln(f,'Moon base: ',a);
       writeln(f,'world id  : ',sys7.world_id);
       writeln(f,'system    : ',sys2.sun_id);
       if (sysowner[sys2.sun_id]=-1) and (sys7.allegiance=raceview) then
          begin
            sysowner[sys2.sun_id]:=raceview;
            system_count:=system_count+1;
          end;
       writeln(f,'world     : ',world_genre[colony_type]);
       writeln(f,'type      : ',colony_genre[sys7.col_class]);
       writeln(f,'race      : ',sys7.race);
       if sys7.allegiance<>65535 then writeln(f,'allegiance: ',sys7.allegiance)
         else writeln(f,'allegiance: Independant');
       writeln(f,'Plan alleg: ',sys2.allegiance);
       factor_pop:=trunc(sys7.pop/10);
       multiplier_pop:=sys7.pop-factor_pop*10;
       if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
       total_pop:=round((multiplier_pop+1)*pop_index[factor_pop+1]*1000);
       if total_pop>999 then writeln(f,'Population: ',round(total_pop/1000),' millions')
          else writeln(f,'Population: ',total_pop,' thousands');
       writeln(f,'starport  : ',starport_genre[sys7.starport]);
       write(f,'law       : ',sys7.law);
       write(f,'     stability : ',sys7.stability:2);
       writeln(f,'     crime     : ',sys7.crime);
       writeln(f,'gnp       : ',sys7.gnp);
       writeln(f,'power     : ',sys7.power);
       writeln(f,'  Facilities: ');
       facilities:=false;
       for b:=1 to 8 do
          begin
             tmp:=1+trunc((b-1)/8);
             if is_set(sys7.misc_char[tmp],b-(tmp-1)*8) then
              begin
                 facilities:=true;
                 writeln(f,'    -',facilities_genre[b]);
              end;
          end;
       if not facilities then writeln(f,'    -None');
       writeln(f);
    end;
  end;
 writeln(f);
 writeln(f);
 writeln(f,'Systems controlled: ',system_count);
 writeln(f);
 writeln(f,'    Id     X     Y     Z  ');
 writeln(f,'--------------------------');
 writeln(f);   
 for a:=0 to 65535 do
   begin
     if sysowner[a]=raceview then
        begin
          seek(sfile,a);
          read(sfile,sys1);
          write(f,a:6,' ');
          write(f,sys1.posX:5,' ');
          write(f,sys1.posY:5,' ');
          writeln(f,sys1.posZ:5,'    ');
        end;
   end;
 close(cyfile);
 close(ctfile);
 close(mfile);
 close(pfile);
 close(afile);
 close(sfile);
 close(f);
end;


procedure TForm8.Edit1Click(Sender: TObject);
var  a:byte;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        namefile:=OpenDialog1.FileName;
        delete(namefile,a-3,4);
        Edit1.Text:=namefile;
      end;
end;

procedure TForm8.Edit3Click(Sender: TObject);
var  a:byte;
begin
   if SaveDialog1.Execute then
      begin
        a:=length(SaveDialog1.FileName);
        outfile:=SaveDialog1.FileName;
        delete(outfile,a-3,4);
        Edit3.Text:=outfile;
      end;
end;

procedure TForm8.OKBtnClick(Sender: TObject);
begin
  main_viewer;
end;

end.
