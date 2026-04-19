{Finder  v1.6  02-2001

v1.57  add characteristics finder
v1.57a compatiblity with Empire v1.57a
v1.57b auto update v1.57b
v1.57c auto update v1.57c, add search for moons
v1.6   add exclude options}


unit Finder;

interface

uses
  Windows, Messages, SysUtils, Classes, Graphics, Controls, Forms, Dialogs,
   recunit, StdCtrls, starunit, ExtCtrls, procunit;

type
  TForm1 = class(TForm)
    Panel1: TPanel;
    Button1: TButton;
    Memo1: TMemo;
    ComboBox1: TComboBox;
    OpenDialog1: TOpenDialog;
    Button2: TButton;
    Label1: TLabel;
    Button3: TButton;
    Panel2: TPanel;
    Label2: TLabel;
    ComboBox2: TComboBox;
    ComboBox3: TComboBox;
    Label3: TLabel;
    Label4: TLabel;
    ComboBox4: TComboBox;
    ComboBox6: TComboBox;
    Label5: TLabel;
    procedure Button1Click(Sender: TObject);
    procedure FormCreate(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
  private
    { Private declarations }
  public
    { Public declarations }
  end;

type
  planet_file= file of planet_record;
  name_file= file of names_record;


var
  Form1: TForm1;
  namefile:string;

implementation

{$R *.DFM}

procedure update_data(namefile:string;old_ver:byte);far;external 'Stardll.dll';




procedure TForm1.Button1Click(Sender: TObject);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    sfile: file of star_record;
    pfile: file of planet_record;
    mfile: file of moon_record;
    afile: file of alien_record;
    aux,aux_str: string;
    system_sizefile,sun_sizefile,moon_sizefile,planet_sizefile:longint;
    a:longint;
    world:byte;
    mass,escape_velocity,gravity:single;
    planetoid,find_world:boolean;
    tmp1,tmp2,tmp3,tmp4,tmp5,tmp6:byte;
begin
    Memo1.Clear;
    world:=ComboBox1.ItemIndex;
    find_world:=false;
    AssignFile (pfile,concat(namefile,'.pln'));
    AssignFile (mfile,concat(namefile,'.mon'));
    reset(mfile);
    reset(pfile);
    planet_sizefile:=filesize(pfile);
    moon_sizefile:=filesize(mfile);
    tmp1:=1+trunc((ComboBox2.ItemIndex-1)/8);
    tmp2:=ComboBox2.ItemIndex-(tmp1-1)*8;
    tmp3:=1+trunc((ComboBox6.ItemIndex-1)/8);
    tmp4:=ComboBox6.ItemIndex-(tmp3-1)*8;
    tmp5:=1+trunc((ComboBox4.ItemIndex-1)/8);
    tmp6:=ComboBox4.ItemIndex-(tmp5-1)*8;
    if ComboBox3.ItemIndex=0 then
       for a:=0 to (planet_sizefile-1) do
         begin
            seek(pfile,a);
            read(pfile,sys2);
            planetoid:=false;
            if (world=6) and (sys2.world_type>14) and (sys2.world_type<19) then
               planetoid:=true;
            if (sys2.world_type=world) or ((world=6) and planetoid) or (ComboBox1.ItemIndex=0) then
               begin
                 if ((ComboBox2.ItemIndex>0) and (is_set(sys2.unusual[tmp1],tmp2))) or
                    (ComboBox2.ItemIndex=0) then
                    begin
                      if ((ComboBox4.ItemIndex>0) and (not is_set(sys2.unusual[tmp5],tmp6))) or
                      (ComboBox4.ItemIndex=0) then
                        begin
                          if ((ComboBox6.ItemIndex>0) and (is_set(sys2.unusual[tmp3],tmp4))) or
                             (ComboBox6.ItemIndex=0) then
                            begin
                              str(a:6,aux_str);
                              aux:=aux_str;
                              gravity_calcul(sys2.density,sys2.diametre,mass,
                                gravity,escape_velocity);
                              str(gravity:0:2,aux_str);
                              aux:=aux+'  '+aux_str+' gees ';
                              str(sys2.temp_avg:4,aux_str);
                              aux:=aux+aux_str+' °C';
                              if is_set(sys2.unusual[3],3) then
                                 aux:=aux+'  '+unusual_genre[19];
                              Memo1.lines.add(aux);
                              find_world:=true;
                            end;
                        end;    
                    end;
               end;
         end
       else
         begin
            ComboBox2.ItemIndex:=0;
            for a:=0 to (moon_sizefile-1) do
              begin
                seek(mfile,a);
                read(mfile,sys3);
                planetoid:=false;
                if (world=6) and (sys3.world_type>14) and (sys3.world_type<19) then
                   planetoid:=true;
                if (sys3.world_type=world) or ((world=6) and planetoid) or (ComboBox1.ItemIndex=0) then
                  begin
                      str(a:6,aux_str);
                      aux:=aux_str;
                      gravity_calcul(sys3.density,sys3.diametre,mass,
                        gravity,escape_velocity);
                      str(gravity:0:2,aux_str);
                      aux:=aux+'  '+aux_str+' gees ';
                      str(sys3.temp_avg:4,aux_str);
                      aux:=aux+aux_str+' °C';
                      Memo1.lines.add(aux);
                      find_world:=true;
                  end;
              end;
         end;
    if not find_world then Memo1.lines.add('None');
    CloseFile(pfile);
    CloseFile(mfile);
end;

procedure TForm1.FormCreate(Sender: TObject);
var a:byte;
    sys5: names_record;
    nfile: name_file;
    pfile: planet_file;
begin
    ComboBox1.Clear;
    ComboBox2.Clear;
    ComboBox3.Clear;
    ComboBox4.Clear;
    ComboBox6.Clear;
    ComboBox1.Items.Add('Any');
    for a:=1 to 23 do
     ComboBox1.Items.Add(world_genre[a]);
    ComboBox2.Items.Add('Any');
    for a:=1 to max_unusual do
      ComboBox2.Items.Add(unusual_genre[a]);
    ComboBox4.Items.Add('None');
    for a:=1 to max_unusual do
      ComboBox4.Items.Add(unusual_genre[a]);
    ComboBox6.Items.Add('None');
    for a:=1 to max_unusual do
      ComboBox6.Items.Add(unusual_genre[a]);

    ComboBox3.Items.Add('Planets');
    ComboBox3.Items.Add('Moons');
    ComboBox1.ItemIndex:=0;
    ComboBox2.ItemIndex:=0;
    ComboBox3.ItemIndex:=0;
    ComboBox4.ItemIndex:=0;
    ComboBox6.ItemIndex:=0;
    OpenDialog1.InitialDir:='.\data';
    if OpenDialog1.Execute then
     begin
       a:=length(OpenDialog1.FileName);
       namefile:=OpenDialog1.FileName;
       delete(namefile,a-3,4);
        assignfile (nfile,concat(namefile,'.nam'));
        assignfile (pfile,concat(namefile,'.pln'));
        reset(nfile);
        reset(pfile);
        read(nfile,sys5);
        if sys5.body_Id<7 then
          begin
            update_data(namefile,sys5.body_Id);
            sys5.body_Id:=7;    {v1.57c}
            write(nfile,sys5);
          end;
        closefile(nfile);
        closefile(pfile);
     end;
     OpenDialog1.InitialDir:='..\data';
     Memo1.Clear;

end;

procedure TForm1.Button2Click(Sender: TObject);
var  a,error_open:byte;
     sys5: names_record;
     nfile: name_file;
     pfile: planet_file;
begin
   if OpenDialog1.Execute then
      begin
        a:=length(OpenDialog1.FileName);
        namefile:=OpenDialog1.FileName;
        delete(namefile,a-3,4);
        assignfile (nfile,concat(namefile,'.nam'));
        assignfile (pfile,concat(namefile,'.pln'));
        reset(nfile);
        reset(pfile);
        read(nfile,sys5);
        if sys5.body_Id<7 then
          begin
            update_data(namefile,sys5.Body_Id);
            sys5.body_Id:=8;    {v1.6}
            write(nfile,sys5);
          end;
        closefile(nfile);
        closefile(pfile);
      end;
end;

procedure TForm1.Button3Click(Sender: TObject);
begin
  Halt(1);
end;

end.
