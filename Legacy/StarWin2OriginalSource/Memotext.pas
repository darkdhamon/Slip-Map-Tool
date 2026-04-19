unit Memotext;

interface

uses WinTypes, WinProcs, Classes, Graphics, Forms, Controls, Buttons,
  StdCtrls, ExtCtrls, recunit;

type
  TBtnRightDlg = class(TForm)
    OKBtn: TBitBtn;
    CancelBtn: TBitBtn;
    Bevel1: TBevel;
    MemoBox: TMemo;
    procedure FormCreate(Sender: TObject;namefile:string);
    procedure OKBtnClick(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  BtnRightDlg: TBtnRightDlg;

implementation

{$R *.DFM}
var auxtext:string;

procedure TBtnRightDlg.FormCreate(Sender: TObject;namefile:string);
begin
   MemoBox.Clear;
   auxtext:=namefile;
   MemoBox.Lines.LoadFromFile(concat(namefile,'.cmt'));
   ShowModal;
end;


procedure TBtnRightDlg.OKBtnClick(Sender: TObject);
begin
   MemoBox.Lines.SaveToFile(concat(auxtext,'.cmt'));
   Exit;
end;

end.
