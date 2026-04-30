#r \"plugins/GridPathfinding/bin/Debug/net8.0/MoonBark.GridPathfinding.dll\"
#r \"cores/MoonBark.Framework/bin/Debug/net8.0/MoonBark.Framework.dll\"
using MoonBark.GridPathfinding;
using MoonBark.Framework.Types;
using Sylves;

var pf = new GridPathfinder();
pf.Initialize(50, 50, (x, y) => false);

var start = new CoreVector2I(0, 0);
var end = new CoreVector2I(10, 0);

bool result = pf.TrySetPath(1, start, end);
Console.WriteLine($\"TrySetPath result: {result}\");

var waypoint = pf.GetCurrentWaypoint(1);
Console.WriteLine($\"First waypoint: {waypoint}\");

int advances = 0;
while (pf.GetCurrentWaypoint(1).HasValue)
{
    pf.AdvanceWaypoint(1);
    advances++;
    var wp = pf.GetCurrentWaypoint(1);
    Console.WriteLine($\"Waypoint {advances}: {wp}\");
    if (advances > 20) break;
}
Console.WriteLine($\"Total advances: {advances}\");
