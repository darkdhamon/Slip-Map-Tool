{Alien logfile Form v1.01  04-99}

unit Unit4;

interface

uses WinTypes, WinProcs, Classes, Graphics, Forms, Controls, Buttons,
  StdCtrls, ExtCtrls, Dialogs;

type
  TBtnRightDlg2 = class(TForm)
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
    ComboBox1: TComboBox;
    Label4: TLabel;
    procedure OKBtnClick(Sender: TObject);
    procedure open_file(Sender: TObject);
    procedure save_log(Sender: TObject);
    procedure HelpBtnClick(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  BtnRightDlg2: TBtnRightDlg2;

implementation

{$R *.DFM}

procedure main_alienlog (filename:string;id:word;rpg_output,output_form:byte;
    var f:text);far; external 'Stardll.dll';

procedure TBtnRightDlg2.OKBtnClick(Sender: TObject);
var filename:string;
    id:word;
    code:integer;
    f:textfile;
    rpg_output,output_form:byte;
begin
    filename:=Edit1.Text;
    Val(Edit3.Text,id,code);
    rpg_output:=ComboBox1.ItemIndex;
    if  SaveDialog1.FilterIndex=2 then
              output_form:=2
        else
              output_form:=0;
    assignfile (f,SaveDialog1.FileName);
    rewrite(f);
    main_alienlog(filename,id,rpg_output,output_form,f);
    closefile(f);
end;

procedure TBtnRightDlg2.open_file(Sender: TObject);
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


procedure TBtnRightDlg2.save_log(Sender: TObject);
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


procedure TBtnRightDlg2.HelpBtnClick(Sender: TObject);
begin
   Application.HelpJump('logfile');
end;

end.
