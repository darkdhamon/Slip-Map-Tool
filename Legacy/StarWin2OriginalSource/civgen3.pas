{Diplomatic relations insert form  v1.0 02-2000}

unit civgen3;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls;

type
  TForm3 = class(TForm)
    Label1: TLabel;
    Edit1: TEdit;
    Label2: TLabel;
    Edit2: TEdit;
    Label3: TLabel;
    ComboBox1: TComboBox;
    Label4: TLabel;
    Edit3: TEdit;
    Button1: TButton;
    Button2: TButton;
    procedure Button2Click(Sender: TObject);
    procedure Button1Click(Sender: TObject);
  private
    { Déclarations privées }
  public
    { Déclarations publiques }
  end;

var
  Form3: TForm3;

implementation

uses civgen1,recunit;

{$R *.DFM}

procedure TForm3.Button2Click(Sender: TObject);
begin
  Form3.Close;
end;

procedure TForm3.Button1Click(Sender: TObject);
var code:integer;
    race1,race2,asize:integer;
    outfile:string;
    sys8: contact_record;
    ctfile: contact_record_file;
    afile: alien_record_file;
    contact_age:byte;
begin
    outfile:=Form1.Edit1.Text;
    assignfile(ctfile,concat(outfile,'.con'));
    assignfile(afile,concat(outfile,'.aln'));
    if FileExists(concat(outfile,'.con')) then reset(ctfile)
      else rewrite(ctfile);
    reset(afile);
    asize:=filesize(afile);
    closefile(afile);
    val(Form3.Edit1.Text,race1,code);
    if code<>0 then
      begin
        ShowMessage('Race 1 value should be an integer');
        closefile(ctfile);
        exit;
      end;
    if race1>asize then
      begin
        ShowMessage('Race 1 Id should be lower');
        closefile(ctfile);
        exit;
      end;
    val(Form3.Edit2.Text,race2,code);
    if code<>0 then
      begin
        ShowMessage('Race 2 value should be an integer');
        closefile(ctfile);
        exit;
      end;
    if race2>asize then
      begin
        ShowMessage('Race 2 Id should be lower');
        closefile(ctfile);
        exit;
      end;
    val(Form3.Edit3.Text,contact_age,code);
    if code<>0 then
      begin
        ShowMessage('The age value should be an integer');
        closefile(ctfile);
        exit;
      end;
    if contact_age<1 then
      begin
        ShowMessage('Alien Id should be higher than 1');
        closefile(ctfile);
        exit;
      end;
    sys8.empire1:=race1;
    sys8.empire2:=race2;
    sys8.relation:=Form3.ComboBox1.ItemIndex+1;
    sys8.age:=contact_age;
    seek(ctfile,filesize(ctfile));
    write(ctfile,sys8);
    closefile(ctfile);
    ShowMessage('Diplomatic relation created');
end;

end.
