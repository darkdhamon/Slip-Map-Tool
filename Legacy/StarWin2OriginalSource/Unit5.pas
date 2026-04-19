{Sector Report Form v1.0  11-98}

unit Unit5;

interface

uses
  SysUtils, WinTypes, WinProcs, Messages, Classes, Graphics, Controls,
  Forms, Dialogs, StdCtrls, Buttons, ExtCtrls,recunit,starunit;

type
  TForm5 = class(TForm)
    Bevel1: TBevel;
    Label1: TLabel;
    Label2: TLabel;
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    Edit1: TEdit;
    Edit2: TEdit;
    OpenDialog1: TOpenDialog;
    SaveDialog1: TSaveDialog;
    CheckBox1: TCheckBox;
    CheckBox2: TCheckBox;
    CheckBox3: TCheckBox;
    CheckBox4: TCheckBox;
    procedure open_sec(Sender: TObject);
    procedure save_map(Sender: TObject);
    procedure OKBtnClick(Sender: TObject);
  private
    { Private-déclarations }
  public
    { Public-déclarations }
  end;

var
  Form5: TForm5;

implementation

{$R *.DFM}
type count_obj=array[1..23] of integer;


procedure main_map(filename,outstr:string;system,alien,summary,empire:boolean);
          far;external 'Stardll.dll';



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



procedure TForm5.open_sec(Sender: TObject);
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


procedure TForm5.save_map(Sender: TObject);
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


procedure TForm5.OKBtnClick(Sender: TObject);
begin
    main_map(Edit1.Text,Edit2.Text,CheckBox1.Checked,CheckBox2.Checked,
      CheckBox3.Checked,CheckBox4.Checked);
end;





end.
