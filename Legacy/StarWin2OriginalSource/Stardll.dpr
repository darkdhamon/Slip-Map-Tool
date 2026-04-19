{Star Dynamic Library v1.6 }

library Stardll;



uses
  PROCUNIT in 'PROCUNIT.PAS',
  Alienlog in 'Alienlog.pas',
  starlog in 'STARLOG.PAS',
  map in 'MAP.PAS',
  rpgconv in 'rpgconv.pas',
  colony1 in 'colony1.pas',
  update in 'update.pas',
  Star in 'Star.pas',
  Alien in 'Alien.pas';

{$R *.RES}
exports
  de index 1,
  power index 2,
  escape_velocity_calcul index 3,
  world_calcul index 4,
  satellites_calcul index 5,
  moon_orbit index 6,
  unusual_calcul index 7,
  Molecule_limit index 8,
  atmos_calcul index 9,
  compos_atmos index 10,
  oxygene_calcul index 11,
  temp_calcul index 12,
  temp_var index 13,
  orbit_calcul index 14,
  rotation_calcul index 15,
  orbital_inclination_calcul index 16,
  eccentricity_calcul index 17,
  axial_tilt_calcul index 18,
  diametre_calcul index 19,
  gravity_calcul index 20,
  hydrosphere_fraction index 21,
  Ice_fraction index 22,
  Cloud_fraction index 23,
  Planet_albedo index 24,
  Determine_albedo_and_surface_temp index 25,
  mine_calcul index 26,
  star_creation index 27,
  orbit_creation index 28,
  main_star index 29,
  main_alien index 30,
  main_alienlog index 31,
  main_starlog index 32,
  main_map index 33,
  stardll_ini index 34,
  main_colony index 35,
  update_data index 36,
  planet_creation index 37,
  moon_creation index 38,
  magnetic_calcul index 39,
  read_ini_files index 40,
  alien_read_ini_files index 41;
begin
end.
 