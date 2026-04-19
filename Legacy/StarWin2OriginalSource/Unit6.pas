{ *** Alien Generator Form v1.21 04-00 ***

     v1.01 Help button
     v1.1  32bit version
     v1.2  Max_spatial parameter
     v1.21 Add SpaceMaster conversion
}

unit Unit6;

interface

uses
  SysUtils, WinTypes, WinProcs, Messages, Classes, Graphics, Controls,
  Forms, Dialogs, StdCtrls, Buttons, ExtCtrls,recunit;

type
  TForm6 = class(TForm)
    Bevel1: TBevel;
    Label2: TLabel;
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    Edit2: TEdit;
    SaveDialog1: TSaveDialog;
    Label1: TLabel;
    Edit1: TEdit;
    Label3: TLabel;
    Edit3: TEdit;
    ComboBox1: TComboBox;
    Label4: TLabel;
    Label5: TLabel;
    ComboBox2: TComboBox;
    CheckBox1: TCheckBox;
    ComboBox3: TComboBox;
    Label6: TLabel;
    procedure savelog(Sender: TObject);
    procedure OKBtnClick(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure HelpBtnClick(Sender: TObject);
  private
    { Private-déclarations }
  public
    { Public-déclarations }
  end;

var
  Form6: TForm6;
  index_rpg:byte;
implementation

{$R *.DFM}

uses alien;

procedure stardll_ini;far; external 'Stardll.dll';
procedure main_alien (temperature:smallint;gravity:real;atmos_comp,world_type:
    byte;new_file:boolean;outstr:string;mother_id:longint;al_disable:boolean;
    max_spatia:byte);
    far;external 'Stardll.dll';
procedure main_alienlog (filename:string;id:word;rpg_output,output_form:byte;
    var f:text);far;external 'Stardll.dll';


procedure TForm6.savelog(Sender: TObject);
var  a:byte;
     namefile:string;
begin
   if SaveDialog1.Execute then
      begin
        a:=length(SaveDialog1.FileName);
        namefile:=SaveDialog1.FileName;
        delete(namefile,a-3,4);
        Edit2.Text:=namefile;
      end;
end;

procedure TForm6.OKBtnClick(Sender: TObject);
var temperature,mess:smallint;
    gravity:real;
    error_del,al_disable:boolean;
    atmos_comp,world_type,output_form:byte;
    code,error_mess:integer;
    outstr,actdir,dirnam,diraln:string;
    afile: file of alien_record;
    nfile:file of names_record;
    pfile: file of planet_record;
    sys2: planet_record;
    f: textfile;
begin
    alien_read_ini_files;
    temperature:=0;
    gravity:=1;
    atmos_comp:=2;
    world_type:=10;
    Val(Edit1.Text,temperature,code);
    if (code<>0) or (temperature<-50) or (temperature>100) then
       begin
         error_mess:=Application.MessageBox('Temperature must be between -50 and 100',
           'Error', 0);
         exit;
       end;
    Val(Edit3.Text,gravity,code);
    if (code<>0) or (gravity<0.5) or (gravity>3) then
       begin
         error_mess:=Application.MessageBox('Gravity must be between 0.5 and 3.0',
           'Error', 0);
         exit;
       end;
    case ComboBox1.ItemIndex of
      0: world_type:=10;
      1: world_type:=8;
      2: world_type:=9;
      3: world_type:=11;
      4: world_type:=12;
      5: world_type:=23;
    end;
    case ComboBox2.ItemIndex of
      0: atmos_comp:=2;
      1: atmos_comp:=4;
      2: atmos_comp:=12;
      3: atmos_comp:=15;
      4: atmos_comp:=19;
      5: atmos_comp:=7;
    end;
    if CheckBox1.State=cbUnchecked then al_disable:=false
      else al_disable:=true;
    outstr:=Edit2.Text;
    GetDir(0,actdir);
    AssignFile(afile,concat(actdir,'\temp0.aln'));
    AssignFile(nfile,concat(actdir,'\temp0.nam'));
    Assignfile(pfile,concat(actdir,'\temp0.pln'));
    rewrite(nfile);
    rewrite(pfile);
    sys2.temp_avg:=temperature;
    sys2.sun_id:=-1; { Means a temporary planet record}
    sys2.atmos:=atmos_comp;
    sys2.pressure:=gravity;  {Note that gravity is recorded under sys2.pressure}
    write(pfile,sys2);
    closefile(nfile);
    closefile(pfile);
    stardll_ini;
    main_alien (temperature,gravity,atmos_comp,world_type,true,concat(actdir,'\temp0'),0,
       al_disable,100);
    if  Form6.SaveDialog1.FilterIndex=2 then
            output_form:=2
      else
             output_form:=0;
    AssignFile(f,SaveDialog1.FileName);
    rewrite(f);
    main_alienlog(concat(actdir,'\temp0'),0,ComboBox3.ItemIndex,
      output_form,f);
    closefile(f);
    erase(nfile);
    erase(pfile);
    erase(afile);
    mess:=Application.MessageBox('Alien Logfile Done','Info',0);
    index_rpg:=Form6.ComboBox3.ItemIndex;
end;

procedure TForm6.FormCreate(Sender: TObject);
begin
   Form6.ComboBox1.Clear;
   Form6.ComboBox2.Clear;
   Form6.ComboBox3.Clear;
   Form6.ComboBox1.Items.Add('Terran');
   Form6.ComboBox1.Items.Add('Arid');
   Form6.ComboBox1.Items.Add('Steppe');
   Form6.ComboBox1.Items.Add('Jungle');
   Form6.ComboBox1.Items.Add('Ocean');
   Form6.ComboBox1.Items.Add('Tundra');
   Form6.ComboBox1.ItemIndex:=0;
   Form6.ComboBox2.Items.Add('Nitrogen/Oxygen');
   Form6.ComboBox2.Items.Add('Nitrogen');
   Form6.ComboBox2.Items.Add('Nitrogen/Chlorine');
   Form6.ComboBox2.Items.Add('Nitrogen/Carbon Dioxyde');
   Form6.ComboBox2.Items.Add('Nitrogen/Sulfuric Acid');
   Form6.ComboBox2.Items.Add('Ammonia');
   Form6.ComboBox2.ItemIndex:=0;
end;


procedure TForm6.HelpBtnClick(Sender: TObject);
begin
   Application.HelpJump('alien_gen');
end;

end.
