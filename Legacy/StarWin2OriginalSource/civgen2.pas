unit civgen2;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls;

type
  TForm2 = class(TForm)
    Label1: TLabel;
    Label2: TLabel;
    Edit1: TEdit;
    Edit2: TEdit;
    Button1: TButton;
    Button2: TButton;
    OpenDialog1: TOpenDialog;
    Label3: TLabel;
    Edit3: TEdit;
    Label4: TLabel;
    Edit4: TEdit;
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Edit1Click(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

var
  Form2: TForm2;

implementation

{$R *.DFM}

uses recunit, civgen1;

procedure TForm2.Button1Click(Sender: TObject);
var  infile,outfile:string;
     afile,out_afile:alien_record_file;
     nfile,out_nfile:name_record_file;
     out_pfile:planet_record_file;
     sys4:alien_record;
     sys5:names_record;
     code:integer;
     age_colony:byte;
     alien_id:word;
     planet_id,name_id:longint;
     find_name:boolean;
begin
   infile:=Form2.Edit1.Text;
   if infile='' then
      begin
        ShowMessage('Error: Sector File is empty');
        Exit;
      end;
   outfile:=Form1.Edit1.Text;
   val(Form2.Edit2.Text,alien_id,code);
   if code>0 then
      begin
        ShowMessage('Alien Id must be an integer');
        Exit;
      end;
   val(Form2.Edit3.Text,planet_id,code);
   if code>0 then
      begin
        ShowMessage('Planet Id must be an integer');
        Exit;
      end;
   val(Form2.Edit4.Text,age_colony,code);
   if code>0 then
      begin
        ShowMessage('Colony Age must be an integer');
        Exit;
      end;
   assignfile(afile,concat(infile,'.aln'));
   assignfile(out_afile,concat(outfile,'.aln'));
   assignfile(out_pfile,concat(outfile,'.pln'));
   assignfile(nfile,concat(infile,'.nam'));
   assignfile(out_nfile,concat(outfile,'.nam'));
   reset(afile);
   reset(out_pfile);
   reset(out_afile);
   reset(nfile);
   reset(out_nfile);
   if alien_id<filesize(afile) then
      begin
        seek(afile,alien_id);
        read(afile,sys4);
        if age_colony>sys4.attrib[15] then
           ShowMessage('Colony age should be lower than spatial age')
        else
          if planet_id<filesize(out_pfile) then
           begin
             sys4.pln_id:=-planet_id;
             sys4.attrib[15]:=age_colony;
             seek(out_afile,filesize(out_afile));
             write(out_afile,sys4);
             find_name:=false;
             if filesize(nfile)>1 then
                begin
                  for name_id:=0 to filesize(nfile) do
                    begin
                      seek(nfile,name_id);
                      read(nfile,sys5);
                      if (sys5.body_type=4) and (sys5.body_Id=alien_id) then
                        begin
                         find_name:=true;
                         break;
                        end;
                    end;
                end;
             if find_name then
                begin
                  seek(out_nfile,filesize(out_nfile));
                  sys5.body_Id:=filesize(out_afile)-1;
                  write(out_nfile,sys5);
                end;
             ShowMessage('Alien race inserted');
             Form2.Close;
           end
           else ShowMessage('Error: Planet Id too high');
      end
      else ShowMessage('Error: Alien Id too high');
   closefile(afile);
   closefile(out_pfile);
   closefile(out_afile);
   closefile(out_nfile);
   closefile(nfile);
end;

procedure TForm2.Button2Click(Sender: TObject);
begin
  Form2.Close;
end;

procedure TForm2.Edit1Click(Sender: TObject);
var  a:byte;
     infile:string;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        infile:=OpenDialog1.FileName;
        delete(infile,a-3,4);
      end;
   Form2.Edit1.Text:=infile;
end;

end.
