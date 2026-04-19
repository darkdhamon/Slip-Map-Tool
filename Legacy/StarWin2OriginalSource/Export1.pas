{Export v1.56 01-2001

  v1.57c bug with 2D sector fix}

unit Export1;

interface

uses
  SysUtils, WinTypes, WinProcs, Messages, Classes, Graphics, Controls,
  Forms, Dialogs, StdCtrls, Buttons, ExtCtrls, recunit,starunit;

type
  TForm1 = class(TForm)
    Label1: TLabel;
    Bevel1: TBevel;
    Edit1: TEdit;
    Label3: TLabel;
    Edit3: TEdit;
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    OpenDialog1: TOpenDialog;
    SaveDialog1: TSaveDialog;
    CheckBox1: TCheckBox;
    procedure FormCreate(Sender: TObject);
    procedure OKBtnClick(Sender: TObject);
    procedure open_sec(Sender: TObject);
    procedure save_log(Sender: TObject);
    procedure CancelBtnClick(Sender: TObject);
  private
    { Private-déclarations }
  public
    { Public-déclarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.DFM}

procedure TForm1.FormCreate(Sender: TObject);
var current_dir,helpdir: string;
begin
  GetDir(0,current_dir);
  helpdir:=concat(current_dir,'\Export.hlp');
  Application.HelpFile:=helpdir;
end;

procedure TForm1.open_sec(Sender: TObject);
var  a:byte;
     namefile:string;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        namefile:=OpenDialog1.FileName;
        delete(namefile,a-3,4);
        Edit1.Text:=namefile;
      end;
end;


procedure TForm1.save_log(Sender: TObject);
var  namefile:string;
begin
   if SaveDialog1.Execute then
      begin
        namefile:=SaveDialog1.FileName;
        Edit3.Text:=namefile;
      end;
end;

procedure file_init(outstr:string;var error_result:boolean);
var nfile: file of names_record;
    sfile: file of star_record;
    error_mess:integer;
begin
{$I-}
 error_result:=true;
 assign (nfile,concat(outstr,'.nam'));
 assign (sfile,concat(outstr,'.sun'));
 reset(sfile);
 if IoResult<>0 then
    begin
       error_mess:=Application.MessageBox('sun file empty or doesn'' exist',
         'Error', 0);
       close(sfile);
       Halt;
    end;
 reset(nfile);
 if IoResult<>0 then
    begin
      error_mess:=Application.MessageBox('nam file doesn''t exist','Error', 0);
      close(nfile);
      Halt;
    end;
 close(sfile);
 close(nfile);
 error_result:=false;
{$I+}
end;

procedure TForm1.OKBtnClick(Sender: TObject);
var outstr,filename,starname:string;
    nfile: file of names_record;
    sfile: file of star_record;
    sys1: star_record;
    sys6: names_record;
    f: TextFile;
    a: longint;
    b,ver: byte;
    mess:integer;
    name_id:longint;
    XCoor,YCoor,ZCoor,TCoor,distance,aux_dist:real; {**TCoor to fix v1.57b 2D bug**}
begin
    randomize;
    outstr:=Edit3.Text;
    filename:=Edit1.Text;
    System.Assign (sfile,concat(filename,'.sun'));
    System.Assign (f,outstr);
    System.Assign (nfile,concat(filename,'.nam')); {**displaced up v1.57c**}
    System.reset(nfile);
    System.reset(sfile);
    System.rewrite(f);
    Screen.Cursor:=crHourglass;
    seek(nfile,0);
    read(nfile,sys6);
    ver:=sys6.body_id;  {**v1.57c added}
    System.close(nfile);
    for a:=0 to (FileSize(sfile)-1) do
        begin
           name_id:=-1;
           starname:='';
           reset(nfile);
           seek(nfile,0);
           if FileSize(nfile)>0 then
              repeat
                read(nfile,sys6);
                if (sys6.body_type=3) and (sys6.body_Id=a)
                   then name_id:=FilePos(nfile);
              until (name_id<>-1) or (Eof(nfile));
           if (FileSize(nfile)>0) and (name_id>-1) then
              begin
                seek(nfile,name_id);
                starname:=sys6.name;
              end;
           if starname='' then Str(a,starname);
           System.Close(nfile);
           read(sfile,sys1);
           for b:=1 to sys1.star_nbr do
              begin
                if sys1.spe_class[b]<15 then
                   begin
                     if sys1.star_nbr=1 then write(f,starname,'/')  { Place name}
                        else if b=1 then write(f,starname,' ',chr(64+b),'/')
                           else write(f,'\');;
                     if sys1.star_nbr=1 then write(f,starname,'/')  { Star name}
                        else write(f,starname,' ',chr(64+b),'/');
                     case b of
                        1: aux_dist:=0;
                        2: aux_dist:=0.01;
                        3: aux_dist:=-0.01;
                     end;
                     {**v1.57c change**}
                     if (CheckBox1.Checked and (ver<7)) then XCoor:=sys1.posX*3.26
                        else XCoor:=sys1.posX*3.26+aux_dist+(random(30)-15)/10;  { Distance from (0,0,0) }
                     {** if CheckBox1.Checked then YCoor:=sys1.posY*3.26
                        else**} YCoor:=sys1.posY*3.26+aux_dist+(random(30)-15)/10;
                     if (CheckBox1.Checked and (ver>6)) then ZCoor:=sys1.posZ*3.26
                        else ZCoor:=sys1.posZ*3.26+aux_dist+(random(30)-15)/10;
                     if (CheckBox1.Checked and (ver<7)) then
                        begin
                          TCoor:=ZCoor;
                          ZCoor:=XCoor;
                          XCoor:=TCoor;
                        end;
                     {**v1.57c change end**}
                     distance:=sqrt(sqr(XCoor)+sqr(YCoor)+sqr(ZCoor));
                     write(f,distance:0:2,'/');
                     write(f,class_spec[sys1.spe_class[b]]); { Spectral type}
                     case sys1.spe_class[b] of
                        1,11: write(f,sys1.type_spec[b],'II');
                        2   : write(f,sys1.type_spec[b],'III');
                        3   : write(f,sys1.type_spec[b],'IV');
                        4..7: write(f,sys1.type_spec[b],'V');
                        8   : write(f,sys1.type_spec[b],'VI');
                        9   : write(f,'VIId');
                        12  : write(f,sys1.type_spec[b],'Ia');
                        10  : write(f,sys1.type_spec[b],'Ib');
                     end;
                     write(f,'/',sys1.mass_star[b]:0:3,'/'); { Star mass}
                     write(f,'/'); { Constellation}
                     write(f,'/'); { Notes}
                     write(f,XCoor:0:2,',',YCoor:0:2,',',ZCoor:0:2);
                     writeln(f);
                   end;
               end;
        end;
    System.Close(f);
    System.Close(sfile);
    Screen.Cursor:=crDefault;
    mess:=Application.MessageBox('Export Done','Info',0);
end;

procedure TForm1.CancelBtnClick(Sender: TObject);
begin
   Halt;
end;


end.
