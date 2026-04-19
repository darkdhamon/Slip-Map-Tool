{System logfile Form v1.0  11-98}

unit Unit3;

interface

uses WinTypes, WinProcs, Classes, Graphics, Forms, Controls, Buttons,
  StdCtrls, ExtCtrls, Dialogs;

type
  TBtnRightDlg1 = class(TForm)
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    Bevel1: TBevel;
    Label1: TLabel;
    Edit1: TEdit;
    Label2: TLabel;
    Edit2: TEdit;
    Label3: TLabel;
    Edit3: TEdit;
    OpenDialog1: TOpenDialog;
    SaveDialog1: TSaveDialog;
    procedure OKBtnClick(Sender: TObject);
    procedure open_sec(Sender: TObject);
    procedure save_log(Sender: TObject);
    procedure HelpBtnClick(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  BtnRightDlg1: TBtnRightDlg1;

implementation

{$R *.DFM}

procedure main_starlog (filename,outstr:string;id:longint;rpg_output,output_form:byte);
    far; external 'Stardll.dll';


procedure TBtnRightDlg1.OKBtnClick(Sender: TObject);
var filename:string;
    id:longint;
    code:integer;
    rpg_output,output_form:byte;
begin
    filename:=Edit1.Text;
    Val(Edit2.Text,id,code);
    rpg_output:=1;
    if  SaveDialog1.FilterIndex=2 then
           output_form:=2
      else
           output_form:=0;
    main_starlog(filename,SaveDialog1.FileName,id,rpg_output,output_form);
end;

procedure TBtnRightDlg1.open_sec(Sender: TObject);
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


procedure TBtnRightDlg1.save_log(Sender: TObject);
var  a:byte;
     namefile:string;
begin
   if SaveDialog1.Execute then
      begin
        a:=length(SaveDialog1.FileName);
        namefile:=SaveDialog1.FileName;
        delete(namefile,a-3,4);
        Edit3.Text:=namefile;
      end;
end;


procedure TBtnRightDlg1.HelpBtnClick(Sender: TObject);
begin
   Application.HelpJump('logfile');
end;

end.
