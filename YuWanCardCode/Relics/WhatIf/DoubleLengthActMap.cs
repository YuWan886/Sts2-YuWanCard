using MegaCrit.Sts2.Core.Map;

namespace YuWanCard.Relics;

public class DoubleLengthActMap : ActMap
{
    private readonly MapPoint?[,] _grid;
    private readonly MapPoint _bossPoint;
    private readonly MapPoint _startingPoint;
    private readonly MapPoint? _secondBossPoint;

    public DoubleLengthActMap(ActMap original)
    {
        int cols = original.GetColumnCount();
        int origRows = original.GetRowCount();
        int newRows = origRows * 2;

        _grid = new MapPoint[cols, newRows];

        // Copy grid points to both halves, skipping special points
        for (int r = 0; r < origRows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var origPoint = original.GetPoint(new MapCoord(c, r));
                if (origPoint == null) continue;
                if (ReferenceEquals(origPoint, original.StartingMapPoint)) continue;
                if (ReferenceEquals(origPoint, original.BossMapPoint)) continue;
                if (ReferenceEquals(origPoint, original.SecondBossMapPoint)) continue;

                var p1 = new MapPoint(c, r)
                {
                    PointType = origPoint.PointType,
                    CanBeModified = origPoint.CanBeModified
                };
                _grid[c, r] = p1;

                var p2 = new MapPoint(c, r + origRows)
                {
                    PointType = origPoint.PointType,
                    CanBeModified = origPoint.CanBeModified
                };
                _grid[c, r + origRows] = p2;
            }
        }

        // Copy graph edges within each half (skip edges to Boss)
        for (int r = 0; r < origRows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var origPoint = original.GetPoint(new MapCoord(c, r));
                if (origPoint == null) continue;
                if (ReferenceEquals(origPoint, original.StartingMapPoint)) continue;
                if (ReferenceEquals(origPoint, original.BossMapPoint)) continue;
                if (ReferenceEquals(origPoint, original.SecondBossMapPoint)) continue;

                var p1 = _grid[c, r];
                var p2 = _grid[c, r + origRows];

                foreach (var child in origPoint.Children)
                {
                    if (child.PointType == MapPointType.Boss) continue;

                    var c1 = _grid[child.coord.col, child.coord.row];
                    var c2 = _grid[child.coord.col, child.coord.row + origRows];
                    if (c1 != null && p1 != null) p1.AddChildPoint(c1);
                    if (c2 != null && p2 != null) p2.AddChildPoint(c2);
                }
            }
        }

        // First half's last row: demote RestSite to Monster (mid-map now)
        for (int c = 0; c < cols; c++)
        {
            var p = _grid[c, origRows - 1];
            if (p != null && p.PointType == MapPointType.RestSite)
            {
                p.PointType = MapPointType.Monster;
                p.CanBeModified = true;
            }
        }

        // Second half's "row 0": demote Ancient copy to Monster (bridge row)
        for (int c = 0; c < cols; c++)
        {
            var p = _grid[c, origRows];
            if (p != null && p.PointType == MapPointType.Ancient)
            {
                p.PointType = MapPointType.Monster;
                p.CanBeModified = true;
            }
        }

        // Starting point (NOT in grid)
        _startingPoint = new MapPoint(cols / 2, 0)
        {
            PointType = MapPointType.Ancient
        };

        // Connect starting point to first fight row
        for (int c = 0; c < cols; c++)
        {
            var p = _grid[c, 1];
            if (p != null)
            {
                _startingPoint.AddChildPoint(p);
                startMapPoints.Add(p);
            }
        }

        // Bridge: connect first half's last row → center node → second half's first fight row
        var bridge = _grid[cols / 2, origRows];
        if (bridge == null)
        {
            bridge = new MapPoint(cols / 2, origRows)
            {
                PointType = MapPointType.Monster,
                CanBeModified = true
            };
            _grid[cols / 2, origRows] = bridge;
        }

        for (int c = 0; c < cols; c++)
        {
            var firstHalfLast = _grid[c, origRows - 1];
            if (firstHalfLast != null)
                firstHalfLast.AddChildPoint(bridge);
        }

        for (int c = 0; c < cols; c++)
        {
            var secondHalfFirst = _grid[c, origRows + 1];
            if (secondHalfFirst != null)
                bridge.AddChildPoint(secondHalfFirst);
        }

        // Final Boss (NOT in grid)
        _bossPoint = new MapPoint(cols / 2, newRows)
        {
            PointType = MapPointType.Boss
        };

        for (int c = 0; c < cols; c++)
        {
            var lastPoint = _grid[c, newRows - 1];
            if (lastPoint != null)
                lastPoint.AddChildPoint(_bossPoint);
        }

        // Second boss
        if (original.SecondBossMapPoint != null)
        {
            _secondBossPoint = new MapPoint(cols / 2, newRows + 1)
            {
                PointType = MapPointType.Boss
            };
            _bossPoint.AddChildPoint(_secondBossPoint);
        }

        MainFile.Logger.Info(
            $"[DoubleLengthActMap] Map doubled: {origRows} → {newRows} rows");
    }

    public override MapPoint BossMapPoint => _bossPoint;
    public override MapPoint StartingMapPoint => _startingPoint;
    public override MapPoint? SecondBossMapPoint => _secondBossPoint;
    protected override MapPoint?[,] Grid => _grid;
}
