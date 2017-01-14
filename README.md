# APNIC.Parser
Simply to take the format which APNIC releases and turn it into a more useful format (http://en.wikipedia.org/wiki/CIDR_notation)

The default options are:
- download source file from ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest
- cache the file at delegated-apnic-extended-latest.cached
- if the cache is older than a day it downloads it again
- output to the console
- output to the clipboard
- limited to the AU location
- limited to the ipv4 type

# Output
## IPV4
It takes a line like the following
```
apnic|AU|ipv4|1.0.0.0|256|20110811|assigned|A91872ED
```
and produces the following output
```
1.0.0.0/24
```
or in verbose mode
```
apnic|AU|ipv4|1.0.0.0|256|20110811|assigned|A91872ED|1.0.0.0|1.0.0.255|1.0.0.0/255.255.255.0|1.0.0.0/24
```

## IPV6

It takes a line like the following
```
apnic|AU|ipv6|2001:360::|35|20011211|allocated|A916A983
```
and produces the following output
```
2001:360::/35
```
or in verbose mode
```
apnic|AU|ipv6|2001:360::|35|20011211|allocated|A916A983|2001:360::/35
```

## ASN

It takes a line like the following
```
apnic|AU|asn|7701|4|19971222|allocated|A9172506
```
and produces the following output
```
7701|7704|4
```
or in verbose mode
```
apnic|AU|asn|7701|4|19971222|allocated|A9172506|7701|7704|4
```

# Options
In this version the options changed.

Available options are:
```
  -f, --filename=VALUE       the name of the filename to read from
  -l, --location=VALUE       a location to limit the details to
  -t, --type=VALUE           a type to limit the details to
  -v                         make the output be in a verbose mode
  -h, --help                 show this message and exit
  -s, --separator=VALUE      provide a custom separator for the entries
  -n                         use new line as a separator
      --location-only        show just the location information, note this is
                               still limited by the type
  -L, --no-location-limit    no limit on the location
  -T, --no-type-limit        no limit on the types
  -X, --no-console-print     dont print the output to the console
  -Z, --no-clipboard-set     dont set the clipboard
  -o, --outfile=VALUE        write the output to a file
  -d, --download             download the file even if not processing file
  -D, --no-download          dont download the file
  -P, --no-process           dont process the file
  -c, --cache-file=VALUE     set the name of the cache file, the default is
                               delegated-apnic-extended-latest.cached
```							   
# Information
An explanation of the source file and it's format can be found here ftp://ftp.apnic.net/pub/apnic/stats/apnic/README.TXT
