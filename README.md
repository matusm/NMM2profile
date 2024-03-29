Nmm2Profile - NMM Data to Profile File Converter
================================================

A standalone command line tool that converts files produced by the [SIOS](https://sios-de.com) NMM-1.
The produced GPS data files are formated according to ISO 25178-7, ISO 25178-71, ISO 25178-72, EUNA 15178 (BCR) and formats proposed by PTB and NPL, respectively.

Some basic leveling procedures can be used. Moreover it is possible to convolute the raw profile with a spherical probe tip of given radius.

## Command Line Usage:  

```
Nmm2Profile inputfile [outputfile] [options]
```

## Options:  

`--channel (-c)` : The channel to be used as topography data. The default is "-LZ+AZ" (the height)

`--scan (-s)` : Scan index for multi-scan files.

`--profile (-p)` : Extract a single profile. If `--profile=0` extract all profiles one by one. 

`--both` : Use the average of forward and backtrace scan data (when present).

`--back` : Use the backtrace scan data only (when present).

`--diff` : Use the difference of forward and backtrace scan data (when present).

`--quiet (-q)` : Quiet mode. No screen output (except for errors).

`--comment` : User supplied string to be included in the metadata (if supported by file type).

`--X0` : Start of trimmed profil, in µm.

`--Xlength` : Length of trimmed profil, in µm.

`--tip` : Convolute file with sherical probe. Parameter is the tip radius in µm.

### Options for height data transformation

`--bias (-b)` : Bias value in µm to be subtracted from the hight values (for `-r5` only).

`--reference (-r)` : The height reference technique, supported values are:

   1: reference to minimum hight value (all values positive)

   2: reference to maximum hight value (all values negative)

   3: reference to average hight value

   4: reference to central hight value (average of minimum and maximum)

   5: reference to user supplied bias value

   6: reference to first value of scan field

   7: reference to last value of scan field

   8: reference to the hight value of the center of scan field (or profile)

   9: reference to connecting line

   10: reference to LSQ line

   11: first apply 9, then 1

   12: first apply 10, then 1

### File type options

`--sdf` : Output BCR format as of ISO 25178-7, ISO 25178-71 and EUNA 15178.

`--smd` : Output SMD format as of ISO 5436-2.

`--x3p` : Output format to XML with schema as of ISO 25178-72. (currently not implemented)

`--sig` : Output format as used by SigmaSurf freeware.

`--txt` : Output basic text file as defined by NPL.

`--prf` : Output PRF format as defined by NPL.

`--prEN` : Output PR format as defined by PTB with English key words.

`--prDE` : Output PR format as defined by PTB with German key words.

