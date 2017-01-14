# APNIC.Parser
Simply to take the format which APNIC releases and turn it into a more useful format

# Running the file
Just running the file will download the latest version from the ftp and output AU location ipv4 address ranges in http://en.wikipedia.org/wiki/CIDR_notation format.

Output is on both the console and put onto the clipboard

# Usage is
apnicparser [filename [location,location,... [ type,type,... [long|l [separator]]]]

filename: can be a relative filename to the application, complete filename, or a web URI, or special name ":"

  If filename is ":" then the filename defaults to ftp://ftp.apnic.net/public/apnic/stats/apnic/delegated-apnic-extended-latest

  Any web site download will cache the results in a local file called delegated-apnic-extended-latest.cached through a temp file delegated-apnic-extended-latest.temp so if either of those exist then they will be deleted

  As long as the cached file is less than a day old it will not be deleted, otherwise it will be re-downloaded

location: is a comma separated list of the locations which should be included in the result

type: is a comma separated list of the types which should be included in the result

long or l : means that a longer more verbose form of the output should be output

separator : the characters to separate the entries, this defaults to a newline but can be replaced with something like  " " to have spaces instead
