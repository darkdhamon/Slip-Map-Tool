{ Sector generator Form  v1.6

   v1.0  11-98
   v1.01 04-99  32-bits version
   v1.1  01-00  Max_spatial parameter
   v1.2  04-00  2D Sector, Phenomenae box
   v1.57c 06-00 2D Sector bug Y,Z variables fix
   v1.6   02-01 Cancel button active during program running, Ini file
 }

unit Unit2;

interface

uses WinTypes, WinProcs, Classes, Graphics, Forms, Controls, Buttons,
  StdCtrls, ExtCtrls, Gauges, Dialogs, recunit;

type
  TBtnRightDlg = class(TForm)
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    Bevel1: TBevel;
    Label1: TLabel;
    Edit1: TEdit;
    Edit2: TLabel;
    Edit3: TEdit;
    Label2: TLabel;
    Edit4: TEdit;
    Label3: TLabel;
    Edit5: TEdit;
    Label4: TLabel;
    Edit6: TEdit;
    Label5: TLabel;
    CheckBox1: TCheckBox;
    CheckBox3: TCheckBox;
    Gauge1: TGauge;
    Label6: TLabel;
    Edit7: TEdit;
    SaveDialog1: TSaveDialog;
    Label7: TLabel;
    CheckBox2: TCheckBox;
    Label8: TLabel;
    Edit8: TEdit;
    Label9: TLabel;
    Edit9: TEdit;
    CheckBox4: TCheckBox;
    procedure OKBtnClick(Sender: TObject);
    procedure opensec(Sender: TObject);
    procedure CancelBtnClick(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  BtnRightDlg: TBtnRightDlg;
  stop_point: boolean;

implementation

{$R *.DFM}

uses procunit, star,alien ;

{** V1.6 uses directly the star and procunit units, fix the floating point bug ?**}




procedure range(var aux:smallint;vmin,vmax:smallint);
begin
if aux<vmin then aux:=vmin;
if aux>vmax then aux:=vmax;
end;

procedure main_sector (new_file,gaia:boolean;vpos_x,vpos_y,vpos_z,density:smallint;
    outstr:string;nbr_sys:longint;al_disable:boolean;system_name:string50;max_spatial:byte);
var countx,county,countz,x,y,z: smallint;
    aux:longint;
    auxstr,finstr:string;
    first,file_arg:boolean;
    nbr_sysX,nbr_sysY,nbr_sysZ:longint;

begin
 randomize;
 stardll_ini;
 first:=true;
 range(vpos_x,-9989,9989);
 range(vpos_y,-9989,9989);
 range(vpos_z,-9989,9989);
 BtnRightDlg.Gauge1.Progress:=0;
 if BtnRightDlg.CheckBox4.checked then nbr_sysZ:=0 else nbr_sysZ:=nbr_sys; {v1.57 c changed}
 nbr_sysY:=nbr_sys;
 nbr_sysX:=nbr_sys;  {v1.57 c changed}
 if nbr_sysZ=0 then BtnRightDlg.Gauge1.MaxValue:=nbr_sys*nbr_sys   { v1.57c bug Y,Z}
    else BtnRightDlg.Gauge1.MaxValue:=nbr_sys*nbr_sys*nbr_sys;
 Screen.Cursor:=crHourglass;
 aux:=0;
 stop_point:=false;
 read_ini_files; {Reading the ini file}
 alien_read_ini_files;
 for countx:= 0 to nbr_sysX do
     for county:= 0 to nbr_sysY do
         for countz:= 0 to nbr_sysZ do
             begin
                aux:=aux+1;
                if random(100)<=density then
                   begin
                      z:=countz+vpos_z;
                      y:=county+vpos_y;
                      x:=countx+vpos_x;
                      if (not new_file) or (not first) then file_arg:=false;
                      main_star(outstr,file_arg,gaia,x,y,z,al_disable,system_name,
                       max_spatial,gaia);
                      first:=false;
                   end;
                BtnRightDlg.Gauge1.Progress:=aux;
                Application.ProcessMessages;
                if stop_point then exit;
             end;
 Screen.Cursor:=crDefault;
end;



procedure TBtnRightDlg.OKBtnClick(Sender: TObject);
var x,y,z:smallint;
    codex,codey,codez,coded,code,mess:integer;
    nbr_sys,aux:longint;
    density,max_spatial:byte;
    new_file,gaia,al_disable:boolean;
    outstr:string;
    system_name:string50;
begin
   Val (Edit3.Text,x,codex);
   Val (Edit4.Text,y,codey);
   Val (Edit5.Text,z,codez);
   Val (Edit6.Text,density,coded);
   system_name:=Edit8.Text;
   outstr:=Edit1.Text;
   if outstr='' then outstr:='starlog';
   if density>100 then density:=100;
   if CheckBox1.State=cbUnchecked then gaia:=false
      else gaia:=true;
   if CheckBox2.State=cbUnchecked then al_disable:=false
      else al_disable:=true;
   if CheckBox3.State=cbUnchecked then new_file:=false
      else new_file:=true;
   val(Edit7.Text,aux,code);
   if code<>0 then
      begin
        Edit7.Text:='10';
        aux:=10;
       end;
   if aux>200 then
      mess:=Application.MessageBox('Size should be inferior to 201','Error',0);
   if aux<1 then
      mess:=Application.MessageBox('Size should be superior to 1','Error',0);
   nbr_sys:=aux-1;
   val(Edit9.Text,max_spatial,code);
   if code<>0 then
      begin
        Edit9.Text:='50';
        max_spatial:=50;
       end;
   if (aux>0) and (aux<201) then
       main_sector (new_file,gaia,x,y,z,density,outstr,nbr_sys,al_disable,
          system_name,max_spatial);
   Gauge1.Progress:=0;
end;

procedure TBtnRightDlg.opensec(Sender: TObject);
var  a:byte;
     namefile:string;
begin
   if SaveDialog1.Execute then
      begin
        a:=length(SaveDialog1.FileName);
        namefile:=SaveDialog1.FileName;
        delete(namefile,a-3,4);
        Edit1.Text:=namefile;
      end;
end;



procedure TBtnRightDlg.CancelBtnClick(Sender: TObject);
begin
   stop_point:=true;
end;

end.
