{Star Win Main menu v1.6   02-2002

   v1.6: Ini file}

unit Unit1;

interface

uses
  SysUtils, WinTypes, WinProcs, Messages, Classes, Graphics, Controls,
  Forms, Dialogs, StdCtrls, Unit2, Unit3, Unit4, Unit5,ExtCtrls, Unit6,
  Unit7, Buttons,IniFiles;

type
  TForm1 = class(TForm)
    Button1: TButton;
    Label1: TLabel;
    Button2: TButton;
    Button3: TButton;
    Image1: TImage;
    Button4: TButton;
    Button5: TButton;
    Button6: TButton;
    Button7: TButton;
    Button8: TButton;
    Button9: TButton;
    Button10: TButton;
    Button11: TButton;
    Button12: TButton;
    Button13: TButton;
    GroupBox1: TGroupBox;
    Button14: TButton;
    Button15: TButton;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
    procedure Button4Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button7Click(Sender: TObject);
    procedure Button9Click(Sender: TObject);
    procedure Button8Click(Sender: TObject);
    procedure Button10Click(Sender: TObject);
    procedure Button11Click(Sender: TObject);
    procedure Button12Click(Sender: TObject);
    procedure Button13Click(Sender: TObject);
    procedure Button14Click(Sender: TObject);
    procedure Button15Click(Sender: TObject);
  private
    { Private-déclarations }
  public
    { Public-déclarations }
  end;

var
  Form1: TForm1;

implementation

{$R *.DFM}

uses FMXUtils, colony_rept, merger1;

var exe_dir: string;

procedure TForm1.Button1Click(Sender: TObject);
begin
    BtnRightDlg.ShowModal;
end;

procedure TForm1.Button2Click(Sender: TObject);
begin
    BtnRightDlg1.ShowModal;
end;

procedure TForm1.Button3Click(Sender: TObject);
begin
    BtnRightDlg2.ComboBox1.Clear;
    BtnRightDlg2.ComboBox1.Items.Add('Standard');
    BtnRightDlg2.ComboBox1.Items.Add('Alternity');
    BtnRightDlg2.ComboBox1.Items.Add('Battlelords');
    BtnRightDlg2.ComboBox1.Items.Add('Gurps');
    BtnRightDlg2.ComboBox1.Items.Add('SpaceMaster');
    BtnRightDlg2.ComboBox1.Items.Add('Fuzion');
    BtnRightDlg2.ComboBox1.ItemIndex:=0;
    BtnRightDlg2.ShowModal;
end;

procedure TForm1.Button4Click(Sender: TObject);
var starIni: TIniFile;
    exe_path:string;
begin
   exe_path:=ExtractFilePath(ParamStr(0));
   starIni := TIniFile.Create(concat(exe_path,'starwin.ini'));
   starIni.WriteInteger('Settings', 'Height', height);
   starIni.WriteInteger('Settings', 'Width', width);
   starIni.WriteInteger('Settings','Left',left);
   starIni.WriteInteger('Settings','Top',top);
   starIni.Free;
   Form1.Close;
end;

procedure TForm1.Button5Click(Sender: TObject);
begin
    Form5.ShowModal;
end;

procedure TForm1.FormCreate(Sender: TObject);
var  starIni: TIniFile;
begin
  exe_dir:=ExtractFilePath(ParamStr(0));
  starIni:=TIniFile.Create(concat(exe_dir,'starwin.ini'));
  height:=starIni.ReadInteger('Settings', 'Height', 324);
  width:=starIni.ReadInteger('Settings', 'Width', 513);
  left:=starIni.ReadInteger('Settings','Left',349);
  top:=starIni.ReadInteger('Settings','Top',201);
  {$I-}
  ChDir('data');
  if IOResult <> 0 then MkDir('data')
   else ChDir('..');
  {$I+}
  {$I-}
  ChDir('log');
  if IOResult <> 0 then
       begin
          with starIni do
           begin
              WriteInteger('Stellar Probabilities', 'Pheno', 1);
              WriteInteger('Stellar Probabilities', 'Misc', 5);
              WriteInteger('Stellar Probabilities', 'A_II', 9);
              WriteInteger('Stellar Probabilities', 'M_III', 20);
              WriteInteger('Stellar Probabilities', 'F_IV', 30);
              WriteInteger('Stellar Probabilities', 'F_V', 115);
              WriteInteger('Stellar Probabilities', 'G_V', 250);
              WriteInteger('Stellar Probabilities', 'K_V', 260);
              WriteInteger('Stellar Probabilities', 'M_V', 260);
              WriteInteger('Stellar Probabilities', 'F_VII', 50);
              WriteInteger('Alien Probabilities', 'Human', 20);
              WriteInteger('Alien Probabilities', 'Animal', 30);
              WriteInteger('Alien Probabilities', 'Reptile', 15);
              WriteInteger('Alien Probabilities', 'Insect', 15);
              WriteInteger('Alien Probabilities', 'Misc', 15);
              WriteInteger('Alien Probabilities', 'Exotic', 5);
           end;
          MkDir('log');
       end
    else ChDir('..');
  if FileExists('altstar.exe') then  button14.enabled:=true else button14.enabled:=false;  
  starIni.Free;
  index_rpg:=0;
  {$I+}
end;

procedure TForm1.Button6Click(Sender: TObject);
begin
   Form6.FormCreate(Sender);
   Form6.ComboBox3.Items.Add('Standard');
   Form6.ComboBox3.Items.Add('Alternity');
   Form6.ComboBox3.Items.Add('Battlelords');
   Form6.ComboBox3.Items.Add('Gurps');
   Form6.ComboBox3.Items.Add('SpaceMaster');
   Form6.ComboBox3.Items.Add('Fuzion');
   Form6.ComboBox3.ItemIndex:=index_rpg;
   Form6.ShowModal;
end;

procedure TForm1.Button7Click(Sender: TObject);
begin
   AboutBox.ShowModal;
end;


procedure TForm1.Button9Click(Sender: TObject);
begin
   ExecuteFile('browser.exe', '', exe_dir, SW_SHOW);
end;

procedure TForm1.Button8Click(Sender: TObject);
begin
   Form8.ShowModal;
end;

procedure TForm1.Button10Click(Sender: TObject);
begin
   ExecuteFile('civgen.exe', '', exe_dir, SW_SHOW);
end;

procedure TForm1.Button11Click(Sender: TObject);
begin
   ExecuteFile('starfind.exe', '', exe_dir, SW_SHOW);
end;

procedure TForm1.Button12Click(Sender: TObject);
begin
   ExecuteFile('EXPORT.exe', '', exe_dir, SW_SHOW);
end;

procedure TForm1.Button13Click(Sender: TObject);
begin
   FormMerger.ShowModal;
end;

procedure TForm1.Button14Click(Sender: TObject);
begin
   ExecuteFile('Altstar.exe', '', exe_dir, SW_SHOW);
end;


procedure TForm1.Button15Click(Sender: TObject);
begin
   if FileExists('army.exe') then ExecuteFile('army.exe', '', exe_dir, SW_SHOW)
     else ShowMessage('Please download and decompress Army Generator in the same directory than Star Generator');
end;

end.
 