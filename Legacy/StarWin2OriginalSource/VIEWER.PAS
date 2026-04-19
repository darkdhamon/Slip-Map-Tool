{Viewer v1.6 by Aina Rasolomalala, 02-2001

  v1.22 add Empire browser
  v1.23 more infos for asteroid belts
  v1.57 add *.emp files, hazel eyes bug fixed
  v1.57a compatiblity with Empire v1.57a
  v1.57b add SpaceMaster, Fuzion conversion, New aliens characteristics
  v1.57c save single systems
  v1.6  compatiblity with stargen v1.6 format (new facilties slot) }




unit Viewer;

interface

uses WinTypes, WinProcs, Classes, Graphics, Forms, Controls, StdCtrls, Tabs,
  Buttons, ExtCtrls, recunit, starunit, Dialogs, Memotext, IniFiles, sysutils;

type
  TMultPageDlg = class(TForm)
    ButtonPanel: TPanel;
    TabSet: TTabSet;
    Notebook: TNotebook;
    GroupBox1: TGroupBox;
    GroupBox2: TGroupBox;
    GroupBox3: TGroupBox;
    Label1: TLabel;
    Label2: TLabel;
    Label3: TLabel;
    Label4: TLabel;
    Label5: TLabel;
    Label6: TLabel;
    Label7: TLabel;
    Label8: TLabel;
    Label9: TLabel;
    Label10: TLabel;
    Label11: TLabel;
    Label12: TLabel;
    Label13: TLabel;
    Label14: TLabel;
    Label15: TLabel;
    Label16: TLabel;
    Label17: TLabel;
    Label18: TLabel;
    Label19: TLabel;
    Label20: TLabel;
    Label21: TLabel;
    Label22: TLabel;
    Label23: TLabel;
    Label24: TLabel;
    Label25: TLabel;
    Label26: TLabel;
    Label27: TLabel;
    Label28: TLabel;
    Label29: TLabel;
    Label30: TLabel;
    Label31: TLabel;
    Label32: TLabel;
    Label33: TLabel;
    Label34: TLabel;
    Label35: TLabel;
    Label36: TLabel;
    Label37: TLabel;
    Label38: TLabel;
    Label39: TLabel;
    Label40: TLabel;
    Edit1: TEdit;
    Edit2: TEdit;
    Edit3: TEdit;
    Edit4: TEdit;
    Edit5: TEdit;
    Edit6: TEdit;
    Edit7: TEdit;
    Edit8: TEdit;
    Edit9: TEdit;
    Edit10: TEdit;
    Edit11: TEdit;
    Edit12: TEdit;
    Edit13: TEdit;
    Edit14: TEdit;
    Edit15: TEdit;
    Edit16: TEdit;
    Edit17: TEdit;
    Edit18: TEdit;
    Edit19: TEdit;
    Edit20: TEdit;
    Edit21: TEdit;
    Edit22: TEdit;
    Edit23: TEdit;
    Edit24: TEdit;
    Edit25: TEdit;
    Edit26: TEdit;
    Edit27: TEdit;
    Edit28: TEdit;
    Edit29: TEdit;
    Edit30: TEdit;
    Edit31: TEdit;
    Edit32: TEdit;
    Edit33: TEdit;
    Edit34: TEdit;
    Edit35: TEdit;
    Edit36: TEdit;
    Edit37: TEdit;
    Edit38: TEdit;
    Memo1: TMemo;
    Memo2: TMemo;
    Button1: TButton;
    Button2: TButton;
    Label41: TLabel;
    Label42: TLabel;
    Label43: TLabel;
    Label44: TLabel;
    Label45: TLabel;
    Label46: TLabel;
    Label47: TLabel;
    Label48: TLabel;
    Label49: TLabel;
    Label50: TLabel;
    Label51: TLabel;
    Label52: TLabel;
    Label53: TLabel;
    Label54: TLabel;
    Label55: TLabel;
    Label56: TLabel;
    Label57: TLabel;
    Label58: TLabel;
    Label59: TLabel;
    Label60: TLabel;
    Label61: TLabel;
    Label62: TLabel;
    Label63: TLabel;
    Label64: TLabel;
    Edit39: TEdit;
    Edit40: TEdit;
    Edit41: TEdit;
    Edit42: TEdit;
    Edit43: TEdit;
    Edit44: TEdit;
    Edit45: TEdit;
    Edit46: TEdit;
    Edit47: TEdit;
    Edit48: TEdit;
    Edit49: TEdit;
    Edit50: TEdit;
    Edit51: TEdit;
    Edit52: TEdit;
    Edit53: TEdit;
    Edit54: TEdit;
    Edit55: TEdit;
    Edit56: TEdit;
    Edit57: TEdit;
    Edit58: TEdit;
    Edit59: TEdit;
    Edit60: TEdit;
    Edit61: TEdit;
    Label65: TLabel;
    Label66: TLabel;
    Label67: TLabel;
    Label68: TLabel;
    Label69: TLabel;
    Label70: TLabel;
    Label71: TLabel;
    Label72: TLabel;
    Edit62: TEdit;
    Edit63: TEdit;
    Edit64: TEdit;
    Edit65: TEdit;
    Edit66: TEdit;
    Edit67: TEdit;
    Edit68: TEdit;
    Edit69: TEdit;
    Label73: TLabel;
    Memo3: TMemo;
    Label74: TLabel;
    Label75: TLabel;
    Label76: TLabel;
    Label77: TLabel;
    Label78: TLabel;
    Memo4: TMemo;
    Edit70: TEdit;
    Edit71: TEdit;
    Edit72: TEdit;
    Edit73: TEdit;
    Label79: TLabel;
    Label80: TLabel;
    Label81: TLabel;
    Label82: TLabel;
    Label83: TLabel;
    Memo5: TMemo;
    Edit74: TEdit;
    Edit75: TEdit;
    Edit76: TEdit;
    Edit77: TEdit;
    Label84: TLabel;
    Label85: TLabel;
    Label86: TLabel;
    Edit78: TEdit;
    Edit79: TEdit;
    Edit80: TEdit;
    GroupBox4: TGroupBox;
    Label87: TLabel;
    Edit81: TEdit;
    Label88: TLabel;
    Edit82: TEdit;
    Edit83: TEdit;
    Label89: TLabel;
    Label90: TLabel;
    Edit84: TEdit;
    Label91: TLabel;
    Edit85: TEdit;
    Edit86: TEdit;
    Label92: TLabel;
    Label93: TLabel;
    Edit87: TEdit;
    Label94: TLabel;
    Edit88: TEdit;
    Label95: TLabel;
    Edit89: TEdit;
    Label96: TLabel;
    Edit90: TEdit;
    Label97: TLabel;
    Edit91: TEdit;
    Label98: TLabel;
    Edit92: TEdit;
    Label99: TLabel;
    Edit93: TEdit;
    Memo6: TMemo;
    Label100: TLabel;
    Memo7: TMemo;
    Memo8: TMemo;
    Label101: TLabel;
    Label102: TLabel;
    Memo9: TMemo;
    Edit94: TEdit;
    Label103: TLabel;
    Memo10: TMemo;
    Label104: TLabel;
    Label105: TLabel;
    Label106: TLabel;
    Label107: TLabel;
    Label108: TLabel;
    Label109: TLabel;
    Label110: TLabel;
    Label111: TLabel;
    Label112: TLabel;
    Label113: TLabel;
    Label114: TLabel;
    Label115: TLabel;
    Edit97: TEdit;
    Edit98: TEdit;
    Edit99: TEdit;
    Edit100: TEdit;
    Edit101: TEdit;
    Edit102: TEdit;
    Edit103: TEdit;
    Edit104: TEdit;
    Edit105: TEdit;
    Edit106: TEdit;
    Edit107: TEdit;
    Edit108: TEdit;
    Label116: TLabel;
    Edit109: TEdit;
    OpenDialog1: TOpenDialog;
    Button3: TButton;
    Edit110: TEdit;
    Button4: TButton;
    Button5: TButton;
    Label117: TLabel;
    Edit111: TEdit;
    Label118: TLabel;
    Label119: TLabel;
    Label120: TLabel;
    Edit112: TEdit;
    Edit113: TEdit;
    Edit114: TEdit;
    Button6: TButton;
    SaveDialog1: TSaveDialog;
    Memo11: TMemo;
    Label121: TLabel;
    Edit95: TEdit;
    Memo12: TMemo;
    Label122: TLabel;
    Label123: TLabel;
    Edit96: TEdit;
    Edit115: TEdit;
    Label124: TLabel;
    Edit116: TEdit;
    Label125: TLabel;
    Edit117: TEdit;
    Label126: TLabel;
    Edit118: TEdit;
    Button7: TButton;
    ComboBox1: TComboBox;
    Label127: TLabel;
    Button8: TButton;
    Button9: TButton;
    Edit119: TEdit;
    Label128: TLabel;
    Button10: TButton;
    GroupBox5: TGroupBox;
    Label129: TLabel;
    Label130: TLabel;
    Label131: TLabel;
    Label132: TLabel;
    Label133: TLabel;
    Label134: TLabel;
    Label135: TLabel;
    Label136: TLabel;
    Label137: TLabel;
    Label138: TLabel;
    Label139: TLabel;
    Label140: TLabel;
    Label141: TLabel;
    Label142: TLabel;
    Edit120: TEdit;
    Edit121: TEdit;
    Edit122: TEdit;
    Edit123: TEdit;
    Edit124: TEdit;
    Edit125: TEdit;
    Edit126: TEdit;
    Edit127: TEdit;
    Edit128: TEdit;
    Edit129: TEdit;
    Edit130: TEdit;
    Edit131: TEdit;
    Edit132: TEdit;
    Edit133: TEdit;
    CheckBox1: TCheckBox;
    Label143: TLabel;
    Edit134: TEdit;
    Button11: TButton;
    Label144: TLabel;
    Edit135: TEdit;
    Label145: TLabel;
    Memo13: TMemo;
    Button12: TButton;
    SaveDialog2: TSaveDialog;
    Button14: TButton;
    Label146: TLabel;
    Edit136: TEdit;
    Label147: TLabel;
    Edit137: TEdit;
    Image1: TImage;
    Image2: TImage;
    Image3: TImage;
    Image4: TImage;
    Image5: TImage;
    Image6: TImage;
    CheckBox2: TCheckBox;
    procedure FormCreate(Sender: TObject);
    procedure TabSetClick(Sender: TObject);
    procedure Button1Click(Sender: TObject);
    procedure Button2Click(Sender: TObject);
    procedure Button3Click(Sender: TObject);
    procedure Button4Click(Sender: TObject);
    procedure Button5Click(Sender: TObject);
    procedure ChangeName4(Sender: TObject);
    procedure Button6Click(Sender: TObject);
    procedure Button7Click(Sender: TObject);
    procedure Button8Click(Sender: TObject);
    procedure Button9Click(Sender: TObject);
    procedure Button10Click(Sender: TObject);
    procedure Button11Click(Sender: TObject);
    procedure Button12Click(Sender: TObject);
    procedure Button14Click(Sender: TObject);
    procedure CheckBox2Click(Sender: TObject);

  private
    { Private declarations }
  public
    { Public declarations }
  end;

type size_int=array[1..5] of integer;

var
  MultPageDlg: TMultPageDlg;
  br_type: byte;
  namefile,name_body,path_pict:string;
  counter,sizefile:size_int;
  id_log:longint;
  economic_power,mi_power,total_pop,trade_bonus: Variant;
  captive_pop,subject_pop,indep_col,indep_pop:Variant;
  colony_nbr,captive_world_nbr,subj_world,moon_nbr,subj_moon: Variant;

implementation

uses empire_viewer,jpeg;

{$R *.DFM}


PROCEDURE gravity_calcul(density:byte;diametre:longint;var mass:single;
   var gravity:single;var escape_velocity:single);far;external 'Stardll.dll';
FUNCTION oxygene_calcul(ice,hydro,atmos_comp:byte;gravity:single):single;far;
   external 'Stardll.dll';
FUNCTION orbit_calcul(orbit:smallint;mass:single):longint;far;external 'Stardll.dll';
PROCEDURE temp_var(var day,night:longint;orbit:smallint;atmos:byte;rot:longint;
          temp:smallint;sys1:star_record;current_star:byte);far;external 'Stardll.dll';
procedure main_starlog (filename,outstr:string;id:longint;rpg_output,output_form:byte);far;external 'Stardll.dll';
procedure main_colony(var economic_power,total_pop,
    trade_bonus,indep_pop,captive_pop,subject_pop,colony_nbr,captive_world_nbr,
    subj_world,moon_nbr,subj_moon,indep_col:Variant ; outstr:string;
    var cyfile:colony_record_file;var ctfile:contact_record_file;
    var afile:alien_record_file);far;external 'Stardll.dll';
procedure update_data(namefile:string;old_ver:byte);far;
    external 'Stardll.dll';
procedure main_alienlog (filename:string;id:word;rpg_output,output_form:byte;var f:text);
    far; external 'Stardll.dll';
FUNCTION magnetic_calcul (resonance:boolean;mass:single;density,world_type:byte;
    rotation_period:longint): single;far;external 'Stardll.dll';


procedure file_disp(outstr:string;var error_open:byte);
var sector_name: string;
    sys5: names_record;
    sys7: colony_record;
    sys9: empire_record;
    pfile: planet_record_file;
    sfile: star_record_file;
    mfile: moon_record_file;
    afile: alien_record_file;
    nfile: name_record_file;
    cyfile: colony_record_file;
    ctfile: contact_record_file;
    efile: empire_record_file;
    a: longint;
begin
  error_open:=0;
  {$I-}
  assignfile (pfile,concat(outstr,'.pln'));
  assignfile (sfile,concat(outstr,'.sun'));
  assignfile (mfile,concat(outstr,'.mon'));
  assignfile (afile,concat(outstr,'.aln'));
  assignfile (nfile,concat(outstr,'.nam'));
  assignfile (cyfile,concat(namefile,'.col'));
  assignfile (ctfile,concat(namefile,'.con'));
  assignfile (efile,concat(namefile,'.emp'));
  reset(sfile);
  sizefile[3]:=filesize(sfile);
  closefile(sfile);
  reset(pfile);
  sizefile[1]:=filesize(pfile);
  closefile(pfile);
  reset(mfile);
  if IoResult<>0 then sizefile[2]:=0
                else sizefile[2]:=filesize(mfile);
  closefile(mfile);
  reset(afile);
  if IoResult<>0 then sizefile[4]:=0
       else sizefile[4]:=filesize(afile);
  closefile(afile);
  reset(cyfile);
  if IoResult<>0 then sizefile[5]:=0
       else sizefile[5]:=filesize(cyfile);
  closefile(cyfile);

  reset(nfile);
  read(nfile,sys5);
  closefile(nfile);
  if (sys5.body_type=0) and (sys5.body_Id=3) then    {Udpating empire infos for v1.56c}
     begin
         main_colony(economic_power,total_pop,trade_bonus,indep_pop,captive_pop,
            subject_pop,colony_nbr,captive_world_nbr,subj_world,moon_nbr,
            subj_moon,indep_col,outstr,cyfile,ctfile,afile);
         reset(cyfile);
         if filesize(cyfile)>0 then
            begin
               for a:=0 to (sizefile[4]-1) do mi_power[a]:=0;
               seek(cyfile,0);
               for a:=0 to (filesize(cyfile)-1) do
                   begin
                      read(cyfile,sys7);
                      mi_power[sys7.allegiance]:=mi_power[sys7.allegiance]+sys7.power;
                   end;
               rewrite(efile);
               for a:=0 to (sizefile[4]-1) do
                  begin
                       sys9.attrib[1]:=economic_power[a];
                       sys9.attrib[2]:=mi_power[a];
                       sys9.attrib[3]:=total_pop[a];
                       sys9.attrib[4]:=trade_bonus[a];
                       sys9.attrib[5]:=captive_pop[a];
                       sys9.attrib[6]:=indep_pop[a];
                       sys9.attrib[7]:=subject_pop[a];
                       sys9.attrib[8]:=colony_nbr[a];
                       sys9.attrib[9]:=captive_world_nbr[a];
                       sys9.attrib[10]:=subj_world[a];
                       sys9.attrib[11]:=moon_nbr[a];
                       sys9.attrib[12]:=subj_moon[a];
                       sys9.attrib[13]:=indep_col[a];
                       write(efile,sys9);
                  end;
           end;
         closefile(cyfile);
     end;
  if (sys5.body_type=0) and (sys5.body_Id<8) then
      begin
        update_data(outstr,sys5.body_Id);
        sys5.body_Id:=8;
        reset(nfile);
        seek(nfile,0);
        write(nfile,sys5);
        closefile(nfile);
      end;

 if sys5.body_type<>0 then error_open:=2
     else
       begin
         sector_name:=concat('Star Browser v1.6 - ',sys5.name);
         MultPageDlg.Caption:=sector_name;
  {$I-}
       end;
end;

function pict_name (world:byte; ringed_planet:boolean) : string;
var aux: string;
begin
    case world of
      1   : aux:='ice.jpg';
      2   : aux:='rock.jpg';
      3,20: aux:='giant.jpg';
      4,21: aux:='hot.jpg';
      5   : aux:='failed.jpg';
      6,15..17: aux:='ast.jpg';
      7   : aux:='chunk.jpg';
      8   : aux:='arid.jpg';
      9   : aux:='steppe.jpg';
      10,22: aux:='garden.jpg';
      11  : aux:='jungle.jpg';
      12  : aux:='ocean.jpg';
      13  : aux:='desert.jpg';
      14  : aux:='glacier.jpg';
      19  : aux:='ring.jpg';
      23  : aux:='tundra.jpg';
      else
        aux:='blank.jpg';
    end;
    if ringed_planet then aux:='ring_'+aux;
    pict_name:=aux;
end;



procedure plan_browser (sys2:planet_record;outstr:string;sys1:star_record;
     current_star:byte);
var  mfile: file of moon_record;
     sys3 : moon_record;
     aux: string;
     aux_radius,oxygene_taux,escape_velocity,gravity,mass,magnetic: single;
     tmp :byte;
     day_temp,night_temp,orbit_period:longint;
     count,auxvar,boiling: smallint;
     equator,polar:smallint;
     ringed_planet:boolean;
begin
    MultPageDlg.Edit112.Text:=sys2.name;
    str(counter[1],aux);
    MultPageDlg.Edit1.Text:=aux;
    str(sys2.sun_id,aux);
    MultPageDlg.Edit118.Text:=aux;
    MultPageDlg.Memo1.Clear;
    MultPageDlg.Memo2.Clear;
    ringed_planet:=false;
    if (sys2.world_type>14) and (sys2.world_type<20) then    { asteroid belts }
	begin
          MultPageDlg.Label5.Caption:='    Width';
	  aux_radius:=abs(sys2.orbit_radius/10);
          str(aux_radius:0:1,aux);
          MultPageDlg.Edit3.Text:=aux+' A.U.';
          str(belt_width[sys2.diametre]:0:3,aux);
          MultPageDlg.Edit5.Text:=aux+' A.U. width';
          str(sys2.density/10:0:1,aux);
	  MultPageDlg.Edit4.Text:=aux;
          MultPageDlg.Edit2.Text:=world_genre[sys2.world_type];
          str(sys2.mine_ress[1],aux);
          MultPageDlg.Edit13.Text:=aux;
          str(sys2.mine_ress[2],aux);
          MultPageDlg.Edit14.Text:=aux;
          str(sys2.mine_ress[3],aux);
          MultPageDlg.Edit15.Text:=aux;
          str(sys2.mine_ress[4],aux);
          MultPageDlg.Edit16.Text:=aux;
          str(sys2.mine_ress[5],aux);
          MultPageDlg.Edit17.Text:=aux;
          str(sys2.temp_avg,aux);
	  MultPageDlg.Edit28.Text:=aux+' °C';
          str(sys2.inclination,aux);
          MultPageDlg.Edit25.Text:=aux+' °';
          MultPageDlg.Edit6.Text:='N/A';
          MultPageDlg.Edit7.Text:='N/A';
          MultPageDlg.Edit8.Text:='N/A';
          MultPageDlg.Edit9.Text:=atmos_genre[sys2.atmos_type];
          MultPageDlg.Edit10.Text:='0 atm';
          MultPageDlg.Edit11.Text:='0 atm';
          MultPageDlg.Edit12.Text:=atmos_comp[sys2.atmos];
          MultPageDlg.Edit18.Text:=water_genre[sys2.water_type];
          MultPageDlg.Edit19.Text:='0';
          str(sys2.hydrography[2],aux);
	  MultPageDlg.Edit20.Text:=aux;
          MultPageDlg.Edit21.Text:='0';
          str(sys2.hydrography[4]/100:0:2,aux);
	  MultPageDlg.Edit22.Text:=aux;
          orbit_period:=orbit_calcul(sys2.orbit_radius,
                        sys1.mass_star[current_star]);
          str(orbit_period,aux);
	  MultPageDlg.Edit23.Text:=aux+' days';
          MultPageDlg.Edit24.Text:='N/A';
          MultPageDlg.Edit26.Text:='N/A';
          MultPageDlg.Edit27.Text:='N/A';
          MultPageDlg.Edit29.Text:='N/A';
          MultPageDlg.Edit30.Text:='N/A';
          MultPageDlg.Edit31.Text:='N/A';
          MultPageDlg.Edit32.Text:='N/A';
          MultPageDlg.Edit33.Text:='N/A';
          MultPageDlg.Edit34.Text:='N/A';
          MultPageDlg.Edit35.Text:='N/A';
          MultPageDlg.Edit36.Text:='-273 °C';
          MultPageDlg.Edit37.Text:='None';
          MultPageDlg.Edit38.Text:='0';
        end
    else begin
          MultPageDlg.Label5.Caption:='Diameter';
          str((sys2.orbit_radius/10):0:1,aux);
          MultPageDlg.Edit3.Text:=aux+' A.U.';
          MultPageDlg.Edit2.Text:=world_genre[sys2.world_type];
          str(sys2.density/10:0:1,aux);
	  MultPageDlg.Edit4.Text:=aux;
          str(sys2.diametre,aux);
	  MultPageDlg.Edit5.Text:=aux+' km';
          gravity_calcul(sys2.density,sys2.diametre,mass,
                        gravity,escape_velocity);
          str(gravity:0:2,aux);
	  MultPageDlg.Edit6.Text:=aux+' g';
          str(mass:0:3,aux);
	  MultPageDlg.Edit7.Text:=aux;
          str(escape_velocity/1E5:0:2,aux);
          MultPageDlg.Edit8.Text:=aux+' km/s';
          MultPageDlg.Edit9.Text:=atmos_genre[sys2.atmos_type];
          str(sys2.pressure:0:3,aux);
          if sys2.pressure>=900 then MultPageDlg.Edit10.Text:='Very High'
	     else MultPageDlg.Edit10.Text:=aux+' atm';
          oxygene_taux:=oxygene_calcul(sys2.hydrography[2],
                       sys2.hydrography[1],sys2.atmos,gravity);
          str(oxygene_taux:0:3,aux);
	  MultPageDlg.Edit11.Text:=aux+' atm';
	  MultPageDlg.Edit12.Text:=atmos_comp[sys2.atmos];
          aux:=water_genre[sys2.water_type];
          MultPageDlg.Edit18.Text:=aux;
          if (sys2.atmos>2) and (sys2.hydrography[1]>0)
             then if is_set(sys2.misc_charac,3) then
                     MultPageDlg.Edit18.Text:=aux+' of tainted liquid water'
	          else
                     MultPageDlg.Edit18.Text:=aux+' of atmosphere related chemical mix';
          str(sys2.hydrography[1],aux);
	  MultPageDlg.Edit19.Text:=aux;
          str(sys2.hydrography[2],aux);
	  MultPageDlg.Edit20.Text:=aux;
          str(sys2.hydrography[3],aux);
	  MultPageDlg.Edit21.Text:=aux;
          str(sys2.hydrography[4]/100:0:2,aux);
	  MultPageDlg.Edit22.Text:=aux;
          orbit_period:=orbit_calcul(sys2.orbit_radius,
                        sys1.mass_star[current_star]);
          str(orbit_period,aux);
	  MultPageDlg.Edit23.Text:=aux+' days';
          str(sys2.rotation_period,aux);
	  MultPageDlg.Edit24.Text:=aux+' hours';
          str(sys2.inclination,aux);
          MultPageDlg.Edit25.Text:=aux+' °';
          str(sys2.eccentricity/1000:0:3,aux);
	  MultPageDlg.Edit26.Text:=aux;
          str(sys2.axial_tilt,aux);
	  MultPageDlg.Edit27.Text:=aux+' °';
          str(sys2.temp_avg,aux);
	  MultPageDlg.Edit28.Text:=aux+' °C';
          str(trunc(sys2.eccentricity*0.03),aux);
	  MultPageDlg.Edit29.Text:='+/- '+aux+' °C';
          str(sys2.axial_tilt*0.6:0:0,aux);
          MultPageDlg.Edit30.Text:='+'+aux+' °C';
          str(sys2.axial_tilt/1.5:0:0,aux);
	  MultPageDlg.Edit31.Text:='-'+aux+' °C';
	  temp_var(day_temp,night_temp,sys2.orbit_radius,
                        sys2.atmos_type,sys2.rotation_period,sys2.temp_avg,
                        sys1,current_star);
          str(day_temp,aux);
          MultPageDlg.Edit32.Text:='+'+aux+' °C';
          str(night_temp,aux);
	  MultPageDlg.Edit33.Text:='-'+aux+' °C';
          if sys2.pressure=0 then boiling:=-273
          else boiling:=trunc(1/(Ln(sys2.pressure)/-5050.5+ 1.0/373)-273);
          str(boiling,aux);
          MultPageDlg.Edit36.Text:=aux+' °C';;
          equator:=round((sys2.axial_tilt*0.3+(sys2.diametre/3000+3)*1.5)/2);
          str(equator,aux);
          if (sys2.world_type=3) or (sys2.world_type=20) then
              MultPageDlg.Edit34.Text:='NA' else MultPageDlg.Edit34.Text:='+'+aux+' °C';
          polar:=round((sys2.axial_tilt+(sys2.diametre/3000+3)*6)/2);
          str(polar,aux);
          if (sys2.world_type=3) or (sys2.world_type=20) then
              MultPageDlg.Edit35.Text:='NA' else MultPageDlg.Edit35.Text:='-'+aux+' °C';
          str(sys2.mine_ress[1],aux);
          MultPageDlg.Edit13.Text:=aux;
          str(sys2.mine_ress[2],aux);
          MultPageDlg.Edit14.Text:=aux;
          str(sys2.mine_ress[3],aux);
          MultPageDlg.Edit15.Text:=aux;
          str(sys2.mine_ress[4],aux);
          MultPageDlg.Edit16.Text:=aux;
          str(sys2.mine_ress[5],aux);
          MultPageDlg.Edit17.Text:=aux;
          magnetic:=magnetic_calcul(is_set(sys2.unusual[3],1),mass,sys2.density,sys2.world_type,
            sys2.rotation_period);
          str(magnetic:0:3,aux);
          MultPageDlg.Edit37.Text:=aux+' gauss';
          for auxvar :=1 to max_unusual do
               begin
                  tmp:=1+trunc((auxvar-1)/8);
                  if is_set(sys2.unusual[tmp],auxvar-(tmp-1)*8) then
                       begin
                            aux:=unusual_genre[auxvar];
                            if auxvar=19 then
                               begin
                                 Str(sys2.alien_id,aux);
                                 aux:=unusual_genre[auxvar]+' ['+aux+']';
                                end;
                            MultPageDlg.Memo2.Lines.Add(aux);
                            count:=count+1;
                       end;
               end;
          if (count=0) then MultPageDlg.Memo2.Lines.Add('None');
          str(sys2.satellites,aux);
          MultPageDlg.Edit38.Text:=aux;
          if (sys2.satellites>0) then
              begin
                 for auxvar:= 1 to sys2.satellites do
                     begin
                       str((sys2.moon_id+auxvar-1),aux);
                       assign(mfile,concat(outstr,'.mon'));
                       reset(mfile);
                       seek(mfile,sys2.moon_id+auxvar-1);
                       read(mfile,sys3);
                       aux:=world_genre2[sys3.world_type]+' ['+aux+']';
                       if sys3.world_type=19 then ringed_planet:=true;
                       MultPageDlg.Memo1.Lines.Add(aux);
                       closefile(mfile);
                     end;
              end;
         end;
     if is_set(sys2.misc_charac,4) then
        begin
          MultPageDlg.Memo2.Lines.Add('Colony');
          MultPageDlg.Button11.Enabled:=true;
        end
      else MultPageDlg.Button11.Enabled:=false;

    {**** picture ****}
    if MultPageDlg.CheckBox2.checked then
    begin
      aux:=pict_name(sys2.world_type,ringed_planet);
      MultPageDlg.Image1.Picture.LoadFromFile(concat(path_pict,aux));
    end;


end;


procedure moon_browser (sys3:moon_record;outstr:string);
var  aux: string;
     oxygene_taux,escape_velocity,gravity,mass: single;
begin
    MultPageDlg.Edit113.Text:=sys3.name;
    str(counter[2],aux);
    MultPageDlg.Edit39.Text:=aux;
    str(sys3.pln_id,aux);
    MultPageDlg.Edit117.Text:=aux;
    if (sys3.world_type>14) and (sys3.world_type<20) then    { asteroid belts }
	begin
          str(sys3.sat_orbit,aux);
          MultPageDlg.Edit41.Text:=aux+' thousands kms';
          MultPageDlg.Edit40.Text:=world_genre[sys3.world_type];
          str(sys3.mine_ress[1],aux);
          MultPageDlg.Edit51.Text:=aux;
          str(sys3.mine_ress[2],aux);
          MultPageDlg.Edit52.Text:=aux;
          str(sys3.mine_ress[3],aux);
          MultPageDlg.Edit53.Text:=aux;
          str(sys3.mine_ress[4],aux);
          MultPageDlg.Edit54.Text:=aux;
          str(sys3.mine_ress[5],aux);
          MultPageDlg.Edit55.Text:=aux;
          str(sys3.temp_avg,aux);
	  MultPageDlg.Edit61.Text:=aux+' °C';
          str(sys3.density/10:0:1,aux);
	  MultPageDlg.Edit42.Text:=aux;
          MultPageDlg.Edit43.Text:='N/A';
          MultPageDlg.Edit44.Text:='N/A';
          MultPageDlg.Edit45.Text:='N/A';
          MultPageDlg.Edit46.Text:='N/A';
          MultPageDlg.Edit47.Text:=atmos_genre[sys3.atmos_type];
          str(sys3.pressure:0:3,aux);
          MultPageDlg.Edit48.Text:=aux+' atm';
          oxygene_taux:=oxygene_calcul(sys3.hydrography[2],
                       sys3.hydrography[1],sys3.atmos,gravity);
          str(oxygene_taux:0:3,aux);
	  MultPageDlg.Edit49.Text:=aux+' atm';
	  MultPageDlg.Edit50.Text:=atmos_comp[sys3.atmos];
          MultPageDlg.Edit56.Text:=water_genre[sys3.water_type];
          str(sys3.hydrography[1],aux);
	  MultPageDlg.Edit57.Text:=aux;
          str(sys3.hydrography[2],aux);
	  MultPageDlg.Edit58.Text:=aux;
          str(sys3.hydrography[3],aux);
	  MultPageDlg.Edit59.Text:=aux;
          str(sys3.hydrography[4]/100:0:2,aux);
	  MultPageDlg.Edit60.Text:=aux;
        end
    else begin
          str(sys3.sat_orbit,aux);
          MultPageDlg.Edit41.Text:=aux+' thousands km';
          MultPageDlg.Edit40.Text:=world_genre[sys3.world_type];
          str(sys3.density/10:0:1,aux);
	  MultPageDlg.Edit42.Text:=aux;
          str(sys3.diametre,aux);
	  MultPageDlg.Edit43.Text:=aux+' km';
          gravity_calcul(sys3.density,sys3.diametre,mass,
                        gravity,escape_velocity);
          str(gravity:0:2,aux);
	  MultPageDlg.Edit44.Text:=aux+' g';
          str(mass:0:3,aux);
	  MultPageDlg.Edit45.Text:=aux;
          str(escape_velocity/1E5:0:2,aux);
          MultPageDlg.Edit46.Text:=aux+' km/s';
          MultPageDlg.Edit47.Text:=atmos_genre[sys3.atmos_type];
          str(sys3.pressure:0:3,aux);
          if sys3.pressure>=900 then MultPageDlg.Edit10.Text:='Very High'
	     else MultPageDlg.Edit48.Text:=aux+' atm';
          oxygene_taux:=oxygene_calcul(sys3.hydrography[2],
                       sys3.hydrography[1],sys3.atmos,gravity);
          str(oxygene_taux:0:3,aux);
	  MultPageDlg.Edit49.Text:=aux+' atm';
	  MultPageDlg.Edit50.Text:=atmos_comp[sys3.atmos];
          aux:=water_genre[sys3.water_type];
          MultPageDlg.Edit56.Text:=aux;
          if (sys3.atmos>2) and (sys3.hydrography[1]>0)
             then if is_set(sys3.misc_charac,3) then
                     MultPageDlg.Edit56.Text:=aux+' of tainted liquid water'
	          else
                     MultPageDlg.Edit56.Text:=aux+' of atmosphere related chemical mix';
          str(sys3.hydrography[1],aux);
	  MultPageDlg.Edit57.Text:=aux;
          str(sys3.hydrography[2],aux);
	  MultPageDlg.Edit58.Text:=aux;
          str(sys3.hydrography[3],aux);
	  MultPageDlg.Edit59.Text:=aux;
          str(sys3.hydrography[4]/100:0:2,aux);
	  MultPageDlg.Edit60.Text:=aux;
          str(sys3.temp_avg,aux);
	  MultPageDlg.Edit61.Text:=aux+' °C';
          str(sys3.mine_ress[1],aux);
          MultPageDlg.Edit51.Text:=aux;
          str(sys3.mine_ress[2],aux);
          MultPageDlg.Edit52.Text:=aux;
          str(sys3.mine_ress[3],aux);
          MultPageDlg.Edit53.Text:=aux;
          str(sys3.mine_ress[4],aux);
          MultPageDlg.Edit54.Text:=aux;
          str(sys3.mine_ress[5],aux);
          MultPageDlg.Edit55.Text:=aux;
         end;

    if MultPageDlg.CheckBox2.checked then
    begin
      aux:=pict_name(sys3.world_type,false);
      MultPageDlg.Image2.Picture.LoadFromFile(concat(path_pict,aux));
    end;

end;

procedure star_browser(sys1:star_record;outstr:string);
var aux:string;
    a,b:byte;
    sys2: planet_record;
    pfile: file of planet_record;
    auxvar:table_20;
begin
 MultPageDlg.Edit114.Text:=sys1.name;
  Str(counter[3],aux);
  MultPageDlg.Edit62.Text:=aux;
  Str(sys1.posX,aux);
  MultPageDlg.Edit63.Text:=aux;
  Str(sys1.posY,aux);
  MultPageDlg.Edit66.Text:=aux;
  Str(sys1.posZ,aux);
  MultPageDlg.Edit64.Text:=aux;
  if sys1.spe_class[1]<16 then
   begin
       Str((sys1.age/10):0:1,aux);
       aux:=aux+' billions years';
       MultPageDlg.Edit69.Text:=aux;
    end;
  for a:=1 to 3 do
    begin
       Str(sys1.type_spec[a],aux);
       if sys1.spe_class[a]<14 then auxvar[a]:=class_spec[sys1.spe_class[a]]+aux
          else auxvar[a]:=class_spec[sys1.spe_class[a]];
       case sys1.spe_class[a] of
                 1,11: aux:=' II';
                 2   : aux:=' III';
                 3   : aux:=' IV';
                 4..7: aux:=' V';
                 8   : aux:=' VI';
                 9   : aux:=' VII';
                 12,13: aux:=' Ia';
                 10  : aux:=' Ib';
                 else aux:='';
        end;
        if sys1.spe_class[a]<14 then auxvar[a]:=auxvar[a]+aux;
     end;
  MultPageDlg.edit65.Text:=auxvar[1];
  MultPageDlg.edit70.Text:=auxvar[2];
  MultPageDlg.edit74.Text:=auxvar[3];
  Str((sys1.companion_dist[1])/10:0:1,aux);
  MultPageDlg.edit73.Text:=aux+' A.U.';
  Str((sys1.companion_dist[2])/10:0:1,aux);
  MultPageDlg.edit77.Text:=aux+' A.U.';
  for a:=1 to 3 do
    if sys1.luminosity[a]<0.001 then Str(sys1.luminosity[a]:0:4,auxvar[a])
       else Str(sys1.luminosity[a]:0:3,auxvar[a]);
  MultPageDlg.edit67.Text:=auxvar[1];
  MultPageDlg.edit71.Text:=auxvar[2];
  MultPageDlg.edit75.Text:=auxvar[3];
  for a:=1 to 3 do
    Str(sys1.mass_star[a]:0:3,auxvar[a]);
  MultPageDlg.edit68.Text:=auxvar[1];
  MultPageDlg.edit72.Text:=auxvar[2];
  MultPageDlg.edit76.Text:=auxvar[3];
  for a:=1 to 3 do
    Str(sys1.planet_nbr[a],auxvar[a]);
  MultPageDlg.edit78.Text:=auxvar[1];
  MultPageDlg.edit79.Text:=auxvar[2];
  MultPageDlg.edit80.Text:=auxvar[3];
  assign(pfile,concat(outstr,'.pln'));
  reset(pfile);
  if sys1.star_nbr<3 then
     begin
       MultPageDlg.edit74.Clear;
       MultPageDlg.edit75.Clear;
       MultPageDlg.edit76.Clear;
       MultPageDlg.edit77.Clear;
       MultPageDlg.edit80.Clear;
     end;
  if sys1.star_nbr=1 then
     begin
       MultPageDlg.edit70.Clear;
       MultPageDlg.edit71.Clear;
       MultPageDlg.edit72.Clear;
       MultPageDlg.edit73.Clear;
       MultPageDlg.edit79.Clear;
     end;
  if sys1.spe_class[1]>14 then
     begin
       MultPageDlg.edit67.Text:='N/A';
       MultPageDlg.edit68.Text:='N/A';
     end;
  MultPageDlg.memo3.Clear;
  MultPageDlg.memo4.Clear;
  MultPageDlg.memo5.Clear;
  for b:=1 to 3 do
    begin
      for a:=1 to sys1.planet_nbr[b] do
           begin
              seek(pfile,sys1.pln_id[b]+a-1);
              read(pfile,sys2);
              Str(sys1.pln_id[b]+a-1,aux);
              aux:=world_genre2[sys2.world_type]+'  [ '+aux+' ]';
              if sys1.planet_nbr[b]=0 then aux:='None';
              case b of
                1: MultPageDlg.memo3.Lines.Add(aux);
                2: MultPageDlg.memo4.Lines.Add(aux);
                3: MultPageDlg.memo5.Lines.Add(aux);
              end;
           end;
    end;

    if MultPageDlg.CheckBox2.checked then
    begin
    case sys1.spe_class[1] of
      1,11,13     : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'bluesun.jpg'));
      2,12        : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'redgsun.jpg'));
      7,8,10      : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'redsun.jpg'));
      3,4         : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'whitesun.jpg'));
      5           : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'yellowsun.jpg'));
      6           : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'orangesun.jpg'));
      9           : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'whitedwarf.jpg'));
      15          : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'nebula.jpg'));
      16          : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'pulsar.jpg'));
      17          : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'blackhole.jpg'));
      19          : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'ion_storm.jpg'));
      22          : MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'rift.jpg'));
      else
        MultPageDlg.Image3.Picture.LoadFromFile(concat(path_pict,'sky.jpg'));
    end;

    if sys1.star_nbr>1 then
       begin
          case sys1.spe_class[2] of
             1,11,13     : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'bluesun.jpg'));
             2,12        : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'redgsun.jpg'));
             7,8,10      : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'redsun.jpg'));
             3,4         : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'whitesun.jpg'));
             5           : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'yellowsun.jpg'));
             6           : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'orangesun.jpg'));
             9           : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'whitedwarf.jpg'));
             15          : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'nebula.jpg'));
             16          : MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'pulsar.jpg'));
             else
                 MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'sky.jpg'));
          end;
       end
     else  MultPageDlg.Image4.Picture.LoadFromFile(concat(path_pict,'blank.jpg')); ;

    if sys1.star_nbr=3 then
       begin
          case sys1.spe_class[3] of
             1,11,13     : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'bluesun.jpg'));
             2,12        : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'redgsun.jpg'));
             7,8,10      : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'redsun.jpg'));
             3,4         : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'whitesun.jpg'));
             5           : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'yellowsun.jpg'));
             6           : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'orangesun.jpg'));
             9           : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'whitedwarf.jpg'));
             15          : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'nebula.jpg'));
             16          : MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'pulsar.jpg'));
             else
                 MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'sky.jpg'));
          end;
       end
     else MultPageDlg.Image5.Picture.LoadFromFile(concat(path_pict,'blank.jpg'));
    end;

  closefile(pfile);
end;

procedure alien_browser(sys4:alien_record);
var ability : boolean;
    aux,result: string;
    a,tmp:byte;
begin
  Str(counter[4],aux);
  MultPageDlg.Edit111.Text:=sys4.name;
  MultPageDlg.Edit81.Text:=aux;
  MultPageDlg.Edit83.Text:=environment_table[sys4.environment_type];
  MultPageDlg.Edit91.Text:=appearance_type[sys4.app_genre];
  MultPageDlg.Edit84.Text:=body_table[sys4.body_type];
  MultPageDlg.Edit90.Text:=body_cover[sys4.body_cover_type];
  MultPageDlg.Memo8.Clear;
  for a:=1 to 16 do
    begin
      tmp:=1+trunc((a-1)/8);
      if is_set(sys4.color_genre[tmp],a-(tmp-1)*8) then
             MultPageDlg.Memo8.Lines.Add(color[a]);
    end;
  MultPageDlg.Edit86.Text:=diet_type[sys4.diet_genre];
  MultPageDlg.Edit87.Text:=reproduction_type[sys4.repro_genre];
  MultPageDlg.Edit88.Text:=repro_methode_type[sys4.repro_meth_genre];
  MultPageDlg.Memo6.Clear;
  Str(sys4.limbs_number,aux);
  MultPageDlg.Edit85.Text:=aux;
  for a:=1 to sys4.limbs_number do
      begin
        Str(a,aux);
        MultPageDlg.Memo6.Lines.Add(aux+' -'+limbs_type[sys4.limbs_genre[a]]);
      end;
  if sys4.limbs_number=0 then MultPageDlg.Memo6.Lines.Add('None');
  Str(sys4.mass:4,aux);
  MultPageDlg.Edit92.Text:=aux+' kg';
  Str(sys4.size_creat:4,aux);
  MultPageDlg.Edit93.Text:=aux+' cm';
  Str(sys4.attrib[1],aux);
  MultPageDlg.Edit97.Text:=aux;
  Str(sys4.attrib[2],aux);
  MultPageDlg.Edit107.Text:=aux;
  Str(sys4.attrib[3],aux);
  MultPageDlg.Edit106.Text:=aux;
  Str(sys4.attrib[4],aux);
  MultPageDlg.Edit105.Text:=aux;
  Str(sys4.attrib[5],aux);
  MultPageDlg.Edit108.Text:=aux;
  Str(sys4.attrib[6],aux);
  MultPageDlg.Edit104.Text:=aux;
  Str(sys4.attrib[8],aux);
  MultPageDlg.Edit98.Text:=aux;
  Str(sys4.attrib[9],aux);
  MultPageDlg.Edit103.Text:=aux;
  Str(sys4.attrib[10],aux);
  MultPageDlg.Edit102.Text:=aux;
  Str(sys4.attrib[11],aux);
  MultPageDlg.Edit101.Text:=aux;
  Str(sys4.attrib[12]*5,aux);
  if sys4.attrib[12]>199 then MultPageDlg.Edit100.Text:='Immortal' else
     MultPageDlg.Edit100.Text:=aux+' years';
  case sys4.attrib[7] of
       1..5 : result:='None';
       6..9: result:='Very Poor';
       10..13: result:='Poor';
       14..16: result:='Fair';
       17..19: result:='Good';
       20 :    result:='Excellent';
  end;
  Str(sys4.attrib[13],aux);
  MultPageDlg.Edit96.Text:=aux;
  Str(sys4.attrib[14],aux);
  MultPageDlg.Edit115.Text:=aux;
  Str(sys4.attrib[15],aux);
  if sys4.attrib[15]>0 then MultPageDlg.Edit116.Text:=aux
     else MultPageDlg.Edit116.Text:='NA';
  MultPageDlg.Edit99.Text:=result;
  MultPageDlg.Edit89.Text:=gov[sys4.gov_type];
  case sys4.devotion of
       0   : result:=' (N)';
       1,2 : result:=' (P)';
       3..6: result:=' (F)';
       7..9: result:=' (G)';
       10  : result:=' (H)';
  end;
  MultPageDlg.Edit119.Text:=religion_genre[sys4.religion]+result;
  ability:=false;
  MultPageDlg.Memo7.Clear;
  for a:=1 to max_abilities do
     begin
        tmp:=1+trunc((a-1)/8);
        if is_set(sys4.table_abil[tmp],a-(tmp-1)*8) then
                           begin
			    MultPageDlg.Memo7.Lines.Add(special_ability[a]);
			    ability:=true;
			   end;
     end;
  if not ability then MultPageDlg.Memo7.Lines.Add('None');
  Str(abs(sys4.pln_id),aux);
  MultPageDlg.Edit82.Text:=aux;
  MultPageDlg.Memo8.Clear;
  for a:=1 to 16 do
      begin
        tmp:=1+trunc((a-1)/8);
        if is_set(sys4.color_genre[tmp],a-(tmp-1)*8) then
	    MultPageDlg.Memo8.Lines.Add(color[a]);
      end;
  aux:='';
  if is_set(sys4.body_char[2],4) then aux:=body_part[12];
  if is_set(sys4.body_char[2],3) then aux:=body_part[11];
  if is_set(sys4.body_char[2],5) then aux:=body_part[13];
  MultPageDlg.Edit109.Text:=aux;
  aux:='';
  if is_set(sys4.body_char[2],6) then aux:=body_part[14];
  if is_set(sys4.body_char[2],7) then aux:=body_part[15];
  if is_set(sys4.body_char[2],8) then aux:=body_part[16];
  if is_set(sys4.body_char[2],3) then aux:=body_part[11];
  MultPageDlg.Edit94.Text:=aux;
  MultPageDlg.Edit95.Text:=hair_part[sys4.hair_char];
  MultPageDlg.Memo12.Clear;
  for a:=1 to 10 do
    begin
      tmp:=1+trunc((a-1)/8);
      if is_set(sys4.body_char[tmp],a-(tmp-1)*8)
         then MultPageDlg.Memo12.Lines.Add(body_part[a]);
    end;
  MultPageDlg.Memo9.Clear;
  if sys4.hair_char<>1 then
     for a:=1 to 16 do
        begin
           tmp:=1+trunc((a-1)/8);
           if is_set(sys4.hair_color[tmp],a-(tmp-1)*8) then
                begin
                  case a of
                    3: MultPageDlg.Memo9.Lines.Add(color[18]);
                    8: MultPageDlg.Memo9.Lines.Add(color[19]);
                    else MultPageDlg.Memo9.Lines.Add(color[a]);
                  end;
                end;
        end;
  MultPageDlg.Memo11.Clear;
  for a:=1 to 14 do
      begin
        tmp:=1+trunc((a-1)/8);
        if is_set(sys4.eye_char[tmp],a-(tmp-1)*8) then
           MultPageDlg.Memo11.Lines.Add(eyes_part[a]);
      end;
  MultPageDlg.Memo10.Clear;
  if not is_set(sys4.eye_char[1],1) then
     begin
       for a:=1 to 16 do
         begin
           tmp:=1+trunc((a-1)/8);
           if is_set(sys4.eye_color[tmp],a-(tmp-1)*8) then
	      begin
                if a=8 then MultPageDlg.Memo10.Lines.Add(color[17])
                  else MultPageDlg.Memo10.Lines.Add(color[a]);
              end;
         end;
     end;
end;

procedure colony_browser (sys7:colony_record;outstr:string);
var sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    pfile: planet_record_file;
    mfile: moon_record_file;
    afile: alien_record_file;
    multiplier_pop:single;
    factor_pop,colony_type:byte;
    total_pop_col:longint;
    aux:string;
    a,tmp:byte;
    facilities:boolean;
const pop_index: ARRAY [1..7] of single=
   (0.01,0.1,1,10,100,1000,10000);
begin
   if sizefile[5]>0 then
   begin
   assign (pfile,concat(namefile,'.pln'));
   assign (mfile,concat(namefile,'.mon'));
   assign (afile,concat(namefile,'.aln'));
   reset(pfile);
   reset(mfile);
   reset(afile);
   Str(counter[5],aux);
   MultPageDlg.Edit120.Text:=aux;
   Str(sys7.world_id,aux);
   if sys7.body_type=1 then
          begin
             aux:=aux+' (Planet)';
             seek(pfile,sys7.world_id);
             read(pfile,sys2);
             colony_type:=sys2.world_type;
          end
     else begin
             aux:=aux+' (Moon)';
             seek(mfile,sys7.world_id);
             read(mfile,sys3);
             colony_type:=sys3.world_type;
          end;
   MultPageDlg.Edit121.Text:=aux;
   MultPageDlg.Edit122.Text:=world_genre[colony_type];
   MultPageDlg.Edit123.Text:=colony_genre[sys7.col_class];
   Str(sys7.race,aux);
   seek(afile,sys7.race);
   read(afile,sys4);
   MultPageDlg.Edit124.Text:=sys4.name+' ['+aux+']';
   Str(sys7.allegiance,aux);
   if sys7.allegiance=65535 then aux:='Independant'else
      begin
        seek(afile,sys7.allegiance);
        read(afile,sys4);
        aux:=sys4.name+' ['+aux+']';
      end;
   MultPageDlg.Edit125.Text:=aux;
   Str(sys7.age,aux);
   aux:=aux+' centuries';
   if sys7.age<>255 then MultPageDlg.Edit126.Text:=aux
      else MultPageDlg.Edit126.Text:='N/A';
   factor_pop:=trunc(sys7.pop/10);
   multiplier_pop:=sys7.pop-factor_pop*10;
   if factor_pop=6 then multiplier_pop:=multiplier_pop/10;
   total_pop_col:=round((multiplier_pop+1)*pop_index[factor_pop+1]*1000);
   if total_pop_col>999 then
        begin
           Str(round(total_pop_col/1000),aux);
           aux:=aux+' millions';
        end
   else begin
           Str(total_pop_col:3,aux);
           aux:=aux+' thousands';
        end;
   MultPageDlg.Edit127.Text:=aux;
   Str(sys7.pop_comp,aux);
   aux:=aux+' %';
   MultPageDlg.Edit135.Text:=aux;
   MultPageDlg.Edit128.Text:=starport_genre[sys7.starport];
   Str(sys7.power,aux);
   MultPageDlg.Edit129.Text:=aux;
   Str(sys7.gnp,aux);
   aux:=aux+' MCr';
   MultPageDlg.Edit130.Text:=aux;
   Str(sys7.law,aux);
   case sys7.law of
     1   : aux:='No law    ['+aux+']';
     2,3 : aux:='Low       ['+aux+']';
     4..6: aux:='Moderate  ['+aux+']';
     7,8 : aux:='High      ['+aux+']';
     9,10: aux:='Extreme   ['+aux+']';
   end;
   MultPageDlg.Edit131.Text:=aux;
   Str(sys7.stability,aux);
   case sys7.stability of
     1   : aux:='Revolt    ['+aux+']';
     2,3 : aux:='Low       ['+aux+']';
     4..6: aux:='Moderate  ['+aux+']';
     7,8 : aux:='High      ['+aux+']';
     9,10: aux:='Very high ['+aux+']';
   end;
   MultPageDlg.Edit132.Text:=aux;
   Str(sys7.crime,aux);
   case sys7.crime of
     1   : aux:='No crime  ['+aux+']';
     2,3 : aux:='Low       ['+aux+']';
     4..6: aux:='Moderate  ['+aux+']';
     7,8 : aux:='High      ['+aux+']';
     9,10: aux:='Very high ['+aux+']';
   end;
   MultPageDlg.Edit133.Text:=aux;
   MultPageDlg.Edit134.Text:=gov[sys7.gov];
   MultPageDlg.Memo13.Clear;
   facilities:=false;
   for a:=1 to 16 do   {** v1.6 new facilities slot ** }
     begin
       tmp:=1+trunc((a-1)/8);
       if is_set(sys7.misc_char[tmp],a-(tmp-1)*8) then
          begin
            MultPageDlg.Memo13.Lines.Add(facilities_genre[a]);
            facilities:=true;
          end;
     end;
   if not facilities then MultPageDlg.Memo13.Lines.Add('None');

   {** v1.6 export/import data **}
   MultPageDlg.Edit136.Text:=eco_genre[sys7.eco_export];
   MultPageDlg.Edit137.Text:=eco_genre[sys7.eco_import];

   closefile(mfile);
   closefile(pfile);
   closefile(afile);
   end
   else
   begin
     MultPageDlg.Memo13.Clear;
     MultPageDlg.Edit120.Text:='';
     MultPageDlg.Edit121.Text:='';
     MultPageDlg.Edit122.Text:='';
     MultPageDlg.Edit123.Text:='';
     MultPageDlg.Edit124.Text:='';
     MultPageDlg.Edit125.Text:='';
     MultPageDlg.Edit126.Text:='';
     MultPageDlg.Edit127.Text:='';
     MultPageDlg.Edit128.Text:='';
     MultPageDlg.Edit129.Text:='';
     MultPageDlg.Edit130.Text:='';
     MultPageDlg.Edit131.Text:='';
     MultPageDlg.Edit132.Text:='';
     MultPageDlg.Edit133.Text:='';
     MultPageDlg.Edit134.Text:='';
     MultPageDlg.Edit135.Text:='';
     MultPageDlg.Edit136.Text:='';
     MultPageDlg.Edit137.Text:='';
   end;
end;


procedure main_viewer (outstr:string);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    sys7: colony_record;
    pfile: planet_record_file;
    sfile: star_record_file;
    mfile: moon_record_file;
    afile: alien_record_file;
    cyfile: colony_record_file;
    current_star,a: byte;
begin
   assignfile (pfile,concat(outstr,'.pln'));
   assignfile (sfile,concat(outstr,'.sun'));
   assignfile (mfile,concat(outstr,'.mon'));
   assignfile (afile,concat(outstr,'.aln'));
   assignfile (cyfile,concat(outstr,'.col'));
   case br_type of
      1: begin              {planets}
           reset(pfile);
           reset(sfile);
           seek(pfile,counter[1]);
           read(pfile,sys2);
           seek(sfile,sys2.sun_id);
           read(sfile,sys1);
           close(pfile);
           close(sfile);
           id_log:=sys2.sun_id;
           for a:= 1 to sys1.star_nbr do
             begin
               if sys1.pln_id[a]<=counter[1] then
                  current_star:=a;
             end;
           plan_browser(sys2,namefile,sys1,current_star);
         end;
      2: begin                {moons}
           reset(pfile);
           reset(sfile);
           reset(mfile);
           seek(mfile,counter[2]);
           read(mfile,sys3);
           seek(pfile,sys3.pln_id);
           read(pfile,sys2);
           seek(sfile,sys2.sun_id);
           read(sfile,sys1);
           close(pfile);
           close(sfile);
           close(mfile);
           id_log:=sys2.sun_id;
           moon_browser (sys3,namefile);
         end;
      3: begin           {stars}
           reset(sfile);
           seek(sfile,counter[3]);
           read(sfile,sys1);
           close(sfile);
           id_log:=counter[3];
           star_browser(sys1,namefile);
         end;
      4: begin         {aliens}
           reset(pfile);
           reset(sfile);
           reset(afile);
           seek(afile,counter[4]);
           read(afile,sys4);
           seek(pfile,abs(sys4.pln_id));
           read(pfile,sys2);
           seek(sfile,sys2.sun_id);
           read(sfile,sys1);
           id_log:=sys2.sun_id;
           close(pfile);
           close(sfile);
           close(afile);
           if sizefile[4]>0 then alien_browser(sys4)
              else
                 begin
                    br_type:=3;
                    MultPageDlg.TabSet.TabIndex:=2;
                    ShowMessage('No alien races');
                 end;
         end;
      5: begin        {Colony}
           reset(pfile);
           reset(mfile);
           reset(cyfile);
           seek(cyfile,counter[5]);
           read(cyfile,sys7);
           if sys7.body_type=1 then
              begin
                seek(pfile,sys7.world_id);
                read(pfile,sys2);
                id_log:=sys2.sun_id;
              end;
           if sys7.body_type=2 then
              begin
                seek(mfile,sys7.world_id);
                read(mfile,sys3);
                seek(pfile,sys3.pln_id);
                read(pfile,sys2);
                id_log:=sys2.sun_id;
              end;
           closefile(pfile);
           closefile(mfile);
           closefile(cyfile);
           colony_browser(sys7,namefile);
         end;

   end;
end;

procedure init_viewer (var error_open:byte);
var a:byte;
    mess_id:smallint;
begin
 file_disp(namefile,error_open);
 case error_open of
   1:begin
          mess_id:=Application.MessageBox('This is a Star v1.54 file, you must convert it','Error', 0);
          Exit;
     end;
   2:begin
          mess_id:=Application.MessageBox('This is not a Star v1.56+ file','Error', 0);
          Exit;
     end;
 end;
 for a:=1 to 5 do counter[a]:=0;
 br_type:=1;
 name_body:='';
 MultPageDlg.TabSet.Tabs := MultPageDlg.Notebook.Pages;
 MultPageDlg.TabSet.TabIndex:=0;
 main_viewer(namefile);
end;


procedure TMultPageDlg.Button1Click(Sender: TObject);
begin
  if counter[br_type]<(sizefile[br_type]-1) then counter[br_type]:=counter[br_type]+1;
  main_viewer(namefile) ;
end;

procedure TMultPageDlg.Button3Click(Sender: TObject);
var  a,error_open:byte;
begin
   if MultPageDlg.OpenDialog1.Execute then
      begin
        a:=length(MultPageDlg.OpenDialog1.FileName);
        namefile:=MultPageDlg.OpenDialog1.FileName;
        delete(namefile,a-3,4);
        init_viewer(error_open);
      end;
end;

procedure TMultPageDlg.FormCreate(Sender: TObject);
var a,error_open:byte;
    starIni: TIniFile;
    exe_path:string;
begin
  { make the number of Tabs = number of Pages,
    and have the same names }
   GetDir(0,path_pict);
   path_pict:=concat(path_pict,'\pictures\');
   MultPageDlg.Image6.Picture.LoadFromFile(concat(path_pict,'browser.jpg'));
   MultPageDlg.OpenDialog1.InitialDir:='.\data';
   if MultPageDlg.OpenDialog1.Execute then
     begin
       a:=length(MultPageDlg.OpenDialog1.FileName);
       namefile:=MultPageDlg.OpenDialog1.FileName;
       delete(namefile,a-3,4);
     end;
   MultPageDlg.OpenDialog1.InitialDir:='..\data';
   init_viewer(error_open);
   if error_open<>0 then Halt;
   exe_path:=ExtractFilePath(ParamStr(0)); {** V1.6 new starwin.ini path **}
   starIni:=TIniFile.Create(concat(exe_path,'starwin.ini'));
   height:=starIni.ReadInteger('Browser Settings', 'Height', 586);
   width:=starIni.ReadInteger('Browser Settings', 'Width', 786);
   left:=starIni.ReadInteger('Browser Settings','Left',21);
   top:=starIni.ReadInteger('Browser Settings','Top',6);
   starIni.Free;

   MultPageDlg.ComboBox1.Clear;
   MultPageDlg.ComboBox1.Items.Add('Standard');
   MultPageDlg.ComboBox1.Items.Add('Alternity');
   MultPageDlg.ComboBox1.Items.Add('Battlelords');
   MultPageDlg.ComboBox1.Items.Add('Gurps');
   MultPageDlg.ComboBox1.Items.Add('SpaceMaster');
   MultPageDlg.ComboBox1.Items.Add('Fuzion');
   MultPageDlg.ComboBox1.Itemindex:=0;
   main_viewer (namefile);
end;

procedure TMultPageDlg.TabSetClick(Sender: TObject);
begin
  { set visible Page to which Tab was clicked }
  Notebook.PageIndex := TabSet.TabIndex;
  br_type:=TabSet.TabIndex+1;
  main_viewer (namefile);
end;

procedure TMultPageDlg.Button2Click(Sender: TObject);
begin
  if counter[br_type]>0 then counter[br_type]:=counter[br_type]-1;
  main_viewer (namefile);
end;


procedure TMultPageDlg.Button4Click(Sender: TObject);
var code:integer;
    go_value:longint;
begin
  val(MultPageDlg.Edit110.Text,go_value,code);
  if go_value<sizefile[br_type] then counter[br_type]:=go_value;
  main_viewer(namefile);
end;

procedure TMultPageDlg.Button5Click(Sender: TObject);
var starIni: TIniFile;
    exe_path: string;
begin
   exe_path:=ExtractFilePath(ParamStr(0));
   starIni := TIniFile.Create(concat(exe_path,'starwin.ini')); {** V1.6 new starwin.ini path **}
   starIni.WriteInteger('Browser Settings', 'Height', height);
   starIni.WriteInteger('Browser Settings', 'Width', width);
   starIni.WriteInteger('Browser Settings','Left',left);
   starIni.WriteInteger('Browser Settings','Top',top);
   starIni.Free;
   Close;
end;


{** v1.6 procedure updated with the new name slot **}
procedure TMultPageDlg.ChangeName4(Sender: TObject);
var sys1: star_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    pfile: planet_record_file;
    sfile: star_record_file;
    mfile: moon_record_file;
    afile: alien_record_file;
begin
   assignfile (pfile,concat(namefile,'.pln'));
   assignfile (sfile,concat(namefile,'.sun'));
   assignfile (mfile,concat(namefile,'.mon'));
   assignfile (afile,concat(namefile,'.aln'));
   case br_type of
      1: begin
           reset(pfile);
           seek(pfile,counter[1]);
           read(pfile,sys2);
           sys2.name:=MultPageDlg.Edit112.Text;
           seek(pfile,counter[1]);
           write(pfile,sys2);
         end;
      2: begin
           reset(mfile);
           seek(mfile,counter[2]);
           read(mfile,sys3);
           sys3.name:=MultPageDlg.Edit113.Text;
           seek(mfile,counter[2]);
           write(mfile,sys3);
         end;
      3: begin
           reset(sfile);
           seek(sfile,counter[3]);
           read(sfile,sys1);
           sys1.name:=MultPageDlg.Edit114.Text;
           seek(sfile,counter[3]);
           write(sfile,sys1);
         end;
      4: begin
           reset(afile);
           seek(afile,counter[4]);
           read(afile,sys4);
           sys4.name:=MultPageDlg.Edit111.Text;
           seek(afile,counter[4]);
           write(afile,sys4);
         end;
   end;
   closefile (pfile);
   closefile (afile);
   closefile (sfile);
   closefile (mfile);
end;


procedure TMultPageDlg.Button6Click(Sender: TObject);
var rpg_output,output_form:byte;
    lognamefile:string;
    f:textfile;
begin
   output_form:=0;
   if MultPageDlg.SaveDialog1.Execute then
      begin
        rpg_output:=ComboBox1.ItemIndex;
        lognamefile:=MultPageDlg.SaveDialog1.FileName;
        if  MultPageDlg.SaveDialog1.FilterIndex=2 then output_form:=2;
        if (not CheckBox1.Checked) and (br_type=4) then
             begin
                assignfile (f,lognamefile);
                rewrite(f);
                main_alienlog(namefile,counter[4],rpg_output,output_form,f);
                closefile(f);
             end
          else main_starlog(namefile,lognamefile,id_log,rpg_output,output_form);
      end;
end;



procedure TMultPageDlg.Button7Click(Sender: TObject);
begin
   BtnRightDlg.FormCreate(Sender,namefile);
end;


procedure TMultPageDlg.Button8Click(Sender: TObject);
begin
  counter[br_type]:=0;
  main_viewer (namefile);
end;

procedure TMultPageDlg.Button9Click(Sender: TObject);
begin
   counter[br_type]:=sizefile[br_type]-1;
  main_viewer (namefile);
end;

procedure TMultPageDlg.Button10Click(Sender: TObject);
var aux:string;
    a:word;
    sys8:contact_record;
    sys9:empire_record;
    ctfile:file of contact_record;
    efile: file of empire_record;
begin
  assignfile(efile,concat(namefile,'.emp'));
  reset(efile);
  if filesize(efile)=0 then
     begin
       closefile(efile);
       ShowMessage('No empire infos');
     end
   else
     begin
        Form1.Show;
        seek(efile,counter[4]);
        read(efile,sys9);
        closefile(efile);
        Str(counter[4],aux);
        Form1.Edit1.Text:=aux;
        Str(sys9.attrib[2],aux);
        Form1.Edit2.Text:=aux;
        Str(sys9.attrib[1],aux);
        aux:=aux+' MCr';
        Form1.Edit3.Text:=aux;
        Str(sys9.attrib[8],aux);
        Form1.Edit4.Text:=aux;
        Str(sys9.attrib[9],aux);
        Form1.Edit5.Text:=aux;
        Str(sys9.attrib[11],aux);
        Form1.Edit6.Text:=aux;
        Str(sys9.attrib[10],aux);
        Form1.Edit7.Text:=aux;
        Str(sys9.attrib[13],aux);
        Form1.Edit12.Text:=aux;
        Str(sys9.attrib[3],aux);
        Form1.Edit8.Text:=aux+' millions';
        Str(sys9.attrib[5],aux);
        Form1.Edit9.Text:=aux+' millions';
        Str(sys9.attrib[7],aux);
        Form1.Edit10.Text:=aux+' millions';
        Str(sys9.attrib[6],aux);
        Form1.Edit13.Text:=aux+' millions';
        Str(sys9.attrib[4],aux);
        aux:=aux+' MCr';
        Form1.Edit11.Text:=aux;
        Form1.Memo1.Clear;
        assignfile (ctfile,concat(namefile,'.con'));
        reset(ctfile);
        if filesize(ctfile)>0 then
           for a:=0 to (filesize(ctfile)-1) do
             begin
                seek(ctfile,a);
                read(ctfile,sys8);
                if sys8.empire1=counter[4] then
                   begin
                      Str(sys8.empire2,aux);
                      aux:=aux+' '+relation_genre[sys8.relation];
                      Form1.Memo1.Lines.Add(aux);
                   end;
                if sys8.empire2=counter[4] then
                   begin
                      Str(sys8.empire1,aux);
                      aux:=aux+' '+relation_genre[sys8.relation];
                      Form1.Memo1.Lines.Add(aux);
                   end;
             end;
        closefile(ctfile);
     end;
end;

procedure TMultPageDlg.Button11Click(Sender: TObject);
var sys7:colony_record;
    cyfile:colony_record_file;
    a: longint;
begin
  assignfile (cyfile,concat(namefile,'.col'));
  reset(cyfile);
  for a:=0 to (filesize(cyfile)-1) do
    begin
      read(cyfile,sys7);
      if (sys7.world_id=counter[1]) and (sys7.body_type=1) then break;
    end;
  counter[5]:=a;
  closefile(cyfile);
  br_type:=5;
  MultPageDlg.TabSet.TabIndex:=4;
  colony_browser(sys7,namefile);
end;

procedure TMultPageDlg.Button12Click(Sender: TObject);
var rpg_output:byte;
    lognamefile:string;
begin
   rpg_output:=ComboBox1.ItemIndex;
   main_starlog(namefile,'',id_log,rpg_output,1);
   ShowMessage('System printed');
end;


procedure TMultPageDlg.Button14Click(Sender: TObject);
var a,b,c:byte;
    outnamefile:string;
    sys1: star_record;
    sys5: names_record;
    sys2: planet_record;
    sys3: moon_record;
    sys4: alien_record;
    pfile,p2file: planet_record_file;
    sfile,s2file: star_record_file;
    mfile,m2file: moon_record_file;
    afile,a2file: alien_record_file;
    efile: empire_record_file;
    nfile: name_record_file;
    cyfile: colony_record_file;
    cfile: textfile;
    ctfile: contact_record_file;
    sunsize,planetsize,moonsize,aliensize: longint;
begin
   if MultPageDlg.SaveDialog2.Execute then
      begin
        a:=length(MultPageDlg.SaveDialog2.FileName);
        outnamefile:=MultPageDlg.SaveDialog2.FileName;
        delete(outnamefile,a-3,4);
        assignfile (pfile,concat(outnamefile,'.pln'));
        assignfile (sfile,concat(outnamefile,'.sun'));
        assignfile (mfile,concat(outnamefile,'.mon'));
        assignfile (afile,concat(outnamefile,'.aln'));
        assignfile (nfile,concat(outnamefile,'.nam'));
        assignfile (cyfile,concat(outnamefile,'.col'));
        assignfile (cfile,concat(outnamefile,'.cmt'));
        assignfile (efile,concat(outnamefile,'.emp'));
        assignfile (ctfile,concat(outnamefile,'.con'));
        assignfile (p2file,concat(namefile,'.pln'));
        assignfile (s2file,concat(namefile,'.sun'));
        assignfile (m2file,concat(namefile,'.mon'));
        assignfile (a2file,concat(namefile,'.aln'));
        {$I-}
        reset(sfile);
        closefile(sfile);
        if IoResult<>0 then begin                {files don't exist}
                               rewrite(pfile);
                               rewrite(sfile);
                               rewrite(mfile);
                               rewrite(afile);
                               rewrite(nfile);
                               rewrite (ctfile);
                               rewrite (cyfile);
                               rewrite(efile);
                               closefile(efile);
                               closefile (ctfile);
                               closefile (cyfile);
                               closefile (afile);
                               closefile (mfile);
                               closefile (sfile);
                               closefile (pfile);
                               sys5.name:='New sector';
                               sys5.body_type:=0;  {to put software version in the *.nam files}
                               sys5.body_Id:=8;     {v1.6}
                               write(nfile,sys5);
                               closefile(nfile);
                               rewrite(cfile);
                               closefile(cfile);
                            end;
        if IoResult<>0 then sunsize:=0
                else sunsize:=filesize(sfile);
        closefile(sfile);
        reset(pfile);
        planetsize:=filesize(pfile);
        closefile(pfile);
        reset(mfile);
        if IoResult<>0 then moonsize:=0
                else moonsize:=filesize(mfile);
        closefile(mfile);
        reset(afile);
        if IoResult<>0 then aliensize:=0
               else aliensize:=filesize(afile);
        closefile(afile);
        {$I+}
        reset(s2file);
        seek (s2file,counter[3]);
        read (s2file,sys1);
        closefile (s2file);
        sys1.allegiance:=65535;
        sys1.code:=0;
        reset(pfile);
        reset(p2file);
        reset(mfile);
        reset(m2file);
        for a:=1 to sys1.star_nbr do
            begin
              for b:=1 to sys1.planet_nbr[a] do
                 begin
                    seek (p2file,sys1.pln_id[a]+b-1);
                    read (p2file,sys2);
                    sys2.allegiance:=65535;
                    unset_bit(sys2.misc_charac,4);
                    unset_bit(sys2.unusual[5],4);
                    sys2.moon_id:=filesize(mfile);
                    for c:=1 to sys2.satellites do
                       begin
                          seek (m2file,sys2.moon_id+c-1);
                          read (m2file,sys3);
                          unset_bit(sys3.misc_charac,4);
                          sys3.pln_id:=filesize(pfile);
                          seek (mfile,filesize(mfile));
                          write(mfile,sys3);
                       end;
                    if is_set(sys2.unusual[3],3) then
                       begin
                          reset(afile);
                          reset(a2file);
                          seek (a2file,sys2.alien_id);
                          read (a2file,sys4);
                          sys4.pln_id:=filesize(pfile);
                          sys2.alien_id:=filesize(afile);
                          seek (afile,filesize(afile));
                          write(afile,sys4);
                          closefile(afile);
                          closefile(a2file);
                       end;
                    sys2.sun_id:=sunsize;   
                    seek (pfile,filesize(pfile));
                    write(pfile,sys2);
                 end;
            end;
        sys1.pln_id[1]:=planetsize;
        sys1.pln_id[2]:=planetsize+sys1.planet_nbr[1];
        sys1.pln_id[3]:=planetsize+sys1.planet_nbr[2];
        closefile(pfile);
        closefile(p2file);
        closefile(mfile);
        closefile(m2file);
        reset(sfile);
        seek (sfile,filesize(sfile));
        write (sfile,sys1);
        closefile(sfile);
      end;
end;


procedure TMultPageDlg.CheckBox2Click(Sender: TObject);
begin
    if not MultPageDlg.CheckBox2.checked then
    begin
       MultPageDlg.Image1.visible:=false;
       MultPageDlg.Image2.visible:=false;
       MultPageDlg.Image3.visible:=false;
       MultPageDlg.Image4.visible:=false;
       MultPageDlg.Image5.visible:=false;
    end
    else
    begin
       MultPageDlg.Image1.visible:=true;
       MultPageDlg.Image2.visible:=true;
       MultPageDlg.Image3.visible:=true;
       MultPageDlg.Image4.visible:=true;
       MultPageDlg.Image5.visible:=true;
    end;
end;

end.

