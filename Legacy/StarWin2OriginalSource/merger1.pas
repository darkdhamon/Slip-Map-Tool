{Sector Merger v1.57b  (c) Aina Rasolomalala 05-2000}

unit merger1;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
  StdCtrls;

type
  TFormMerger = class(TForm)
    Label1: TLabel;
    Label2: TLabel;
    Edit1: TEdit;
    Edit2: TEdit;
    Button1: TButton;
    Button2: TButton;
    OpenDialog1: TOpenDialog;
    OpenDialog2: TOpenDialog;
    procedure Edit2Click(Sender: TObject);
    procedure Edit1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button1Click(Sender: TObject);
  private
    { Déclarations privées }
  public
    { Déclarations publiques }
  end;

var
  FormMerger: TFormMerger;

implementation

{$R *.DFM}

uses recunit;

var sourcefile,addfile:string;

procedure TFormMerger.Edit2Click(Sender: TObject);
var a:byte;
begin
   if OpenDialog2.Execute then
      begin
        addfile:=OpenDialog2.FileName;
        a:=length(OpenDialog2.FileName);
        delete(addfile,a-3,4);
        Edit2.Text:=addfile;
      end;
end;


procedure TFormMerger.Edit1Click(Sender: TObject);
var  a:byte;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        sourcefile:=OpenDialog1.FileName;
        delete(sourcefile,a-3,4);
        Edit1.Text:=sourcefile;
      end;
end;

procedure TFormMerger.Button2Click(Sender: TObject);
begin
   close;
end;

procedure TFormMerger.Button1Click(Sender: TObject);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    pfile,pfile2: planet_record_file;
    sfile,sfile2: star_record_file;
    mfile,mfile2: moon_record_file;
    afile,afile2: alien_record_file;
    a: longint;
    ssize,psize,asize,msize,auxsize: longint;
    f,f2:textfile;
    st:string;
begin
   assignfile (pfile,concat(sourcefile,'.pln'));
   assignfile (sfile,concat(sourcefile,'.sun'));
   assignfile (mfile,concat(sourcefile,'.mon'));
   assignfile (afile,concat(sourcefile,'.aln'));
   assignfile (f,concat(sourcefile,'.cmt'));
   assignfile (pfile2,concat(addfile,'.pln'));
   assignfile (sfile2,concat(addfile,'.sun'));
   assignfile (mfile2,concat(addfile,'.mon'));
   assignfile (afile2,concat(addfile,'.aln'));
   assignfile (f2,concat(addfile,'.cmt'));
   reset(sfile);
   reset(sfile2);
   reset(pfile);
   reset(pfile2);
   reset(afile);
   reset(afile2);
   reset(mfile);
   reset(mfile2);
   ssize:=filesize(sfile);
   psize:=filesize(pfile);
   asize:=filesize(afile);
   msize:=filesize(mfile);
   seek(sfile,ssize);
   for a:=0 to (filesize(sfile2)-1) do
       begin
         seek(sfile2,a);
         read(sfile2,sys1);
         sys1.pln_id[1]:=sys1.pln_id[1]+psize;
         sys1.pln_id[2]:=sys1.pln_id[2]+psize;
         sys1.pln_id[3]:=sys1.pln_id[3]+psize;
         if sys1.allegiance<>65535 then sys1.allegiance:=sys1.allegiance+asize;
         write(sfile,sys1);
       end;
   seek(pfile,psize);
   for a:=0 to (filesize(pfile2)-1) do
       begin
         seek(pfile2,a);
         read(pfile2,sys2);
         sys2.sun_id:=sys2.sun_id+ssize;
         if is_set(sys2.unusual[3],3) then sys2.alien_id:=sys2.alien_id+asize;
         if sys2.satellites>0 then sys2.moon_id:=sys2.moon_id+msize;
         if (sys2.allegiance<>65535) and (is_set(sys2.misc_charac,4)) then
            sys2.allegiance:=sys2.allegiance+asize;
         write(pfile,sys2);
       end;
   seek(mfile,msize);
   for a:=0 to (filesize(mfile2)-1) do
       begin
         seek(mfile2,a);
         read(mfile2,sys3);
         sys3.pln_id:=sys3.pln_id+psize;
         write(mfile,sys3);
       end;
   seek(afile,asize);
   for a:=0 to (filesize(afile2)-1) do
       begin
         seek(afile2,a);
         read(afile2,sys4);
         if sys4.pln_id>-1 then sys4.pln_id:=sys4.pln_id+psize
            else sys4.pln_id:=sys4.pln_id-psize;
         write(afile,sys4);
       end;
   closefile(sfile);
   closefile(sfile2);
   closefile(pfile);
   closefile(pfile2);
   closefile(afile);
   closefile(afile2);
   closefile(mfile);
   closefile(mfile2);
   append(f);
   reset(f2);
   repeat
     readln(f2,st);
     writeln(f,st);
   until eof(f2);
   flush(f);
   closefile(f);
   closefile(f2);
   ShowMessage('The 2 sectors have been merged');
end;

end.
