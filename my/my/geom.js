// Generated by CoffeeScript 1.7.1
var __hasProp = {}.hasOwnProperty,
  __extends = function(child, parent) { for (var key in parent) { if (__hasProp.call(parent, key)) child[key] = parent[key]; } function ctor() { this.constructor = child; } ctor.prototype = parent.prototype; child.prototype = new ctor(); child.__super__ = parent.prototype; return child; };

require(function() {
  var Point, PointSet, distance, distance2, eps, interpolatePoint, interpolatePointWeighted, raycast;
  Point = (function() {
    function Point(x, y) {
      this.x = x;
      this.y = y;
    }

    Point.prototype.plus = function(p) {
      return new Point(this.x + p.x, this.y + p.y);
    };

    Point.prototype.minus = function(p) {
      return new Point(this.x - p.x, this.y - p.y);
    };

    Point.prototype.mult = function(sx, sy) {
      return new Point(this.x * sx, this.y * sy);
    };

    Point.prototype.distance = function(p) {
      return distance(this, p);
    };

    Point.prototype.distance2 = function(p) {
      return distance2(this, p);
    };

    Point.prototype.eq = function(p) {
      return this.x === p.x && this.y === p.y;
    };

    Point.prototype.toString = function() {
      return "x=" + this.x + ",y=" + this.y;
    };

    Point.prototype.toJSON = function() {
      return {
        x: this.x,
        y: this.y
      };
    };

    return Point;

  })();
  globals.pt = function(x, y) {
    return new Point(x, y);
  };
  distance = function(_arg, _arg1) {
    var x, x0, x1, y, y0, y1;
    x0 = _arg.x, y0 = _arg.y;
    x1 = _arg1.x, y1 = _arg1.y;
    x = x1 - x0;
    y = y1 - y0;
    return Math.sqrt(x * x + y * y);
  };
  distance2 = function(_arg, _arg1) {
    var x, x0, x1, y, y0, y1;
    x0 = _arg.x, y0 = _arg.y;
    x1 = _arg1.x, y1 = _arg1.y;
    x = x1 - x0;
    y = y1 - y0;
    return x * x + y * y;
  };
  raycast = function(_arg, _arg1, radius, cb) {
    var cellx, celly, dx, dy, fx, fy, oldx, oldy, qx, qy, sx, sy, x, x0, x1, xx, y, y0, y1, yy, _i, _j;
    sx = _arg.x, sy = _arg.y;
    fx = _arg1.x, fy = _arg1.y;
    dx = fx - sx;
    dy = fy - sy;
    qx = dx / dy;
    qy = dy / dx;
    if (dx !== 0) {
      xx = Math.sqrt(radius * radius / (1 + qy * qy));
      if (dx < 0) {
        xx = -xx;
      }
      yy = xx * qy;
      fx = sx + xx;
      fy = sy + yy;
    } else {
      if (dy > 0) {
        fy = sy + radius;
      } else {
        fy = sy - radius;
      }
    }
    if (Math.abs(dx) > Math.abs(dy)) {
      x0 = dx > 0 ? Math.ceil(sx) : Math.floor(sx);
      x1 = dx > 0 ? Math.floor(fx) : Math.ceil(fx);
      oldx = x0;
      oldy = Math.floor(sy);
      for (x = _i = x0; x0 <= x1 ? _i <= x1 : _i >= x1; x = x0 <= x1 ? ++_i : --_i) {
        y = sy + (x - sx) * qy;
        cellx = dx > 0 ? x : x - 1;
        if (isInt(y)) {
          celly = dy > 0 ? Math.round(y) : Math.round(y) - 1;
        } else {
          celly = Math.floor(y);
          if (celly !== oldy) {
            if ((cb(pt(oldx, celly))) === false) {
              return false;
            }
          }
        }
        if ((cb(pt(cellx, celly))) === false) {
          return false;
        }
        oldx = cellx;
        oldy = celly;
      }
    } else {
      y0 = dy > 0 ? Math.ceil(sy) : Math.floor(sy);
      y1 = dy > 0 ? Math.floor(fy) : Math.ceil(fy);
      oldx = Math.floor(sx);
      oldy = y0;
      for (y = _j = y0; y0 <= y1 ? _j <= y1 : _j >= y1; y = y0 <= y1 ? ++_j : --_j) {
        x = sx + (y - sy) * qx;
        celly = dy > 0 ? y : y - 1;
        if (isInt(x)) {
          cellx = dx > 0 ? Math.round(x) : Math.round(x) - 1;
        } else {
          cellx = Math.floor(x);
          if (cellx !== oldx) {
            if ((cb(pt(cellx, oldy))) === false) {
              return false;
            }
          }
        }
        if ((cb(pt(cellx, celly))) === false) {
          return false;
        }
        oldx = cellx;
        oldy = celly;
      }
    }
    return true;
  };
  PointSet = (function(_super) {
    __extends(PointSet, _super);

    function PointSet() {
      return PointSet.__super__.constructor.apply(this, arguments);
    }

    PointSet.prototype.key = function(_arg) {
      var x, y;
      x = _arg.x, y = _arg.y;
      return "" + x + "," + y;
    };

    return PointSet;

  })(GenericSet);
  interpolatePoint = function(t, points) {
    var p, xs, ys;
    xs = (function() {
      var _i, _len, _results;
      _results = [];
      for (_i = 0, _len = points.length; _i < _len; _i++) {
        p = points[_i];
        _results.push(p.x);
      }
      return _results;
    })();
    ys = (function() {
      var _i, _len, _results;
      _results = [];
      for (_i = 0, _len = points.length; _i < _len; _i++) {
        p = points[_i];
        _results.push(p.y);
      }
      return _results;
    })();
    return pt(interpolate(t, xs), interpolate(t, ys));
  };
  eps = 1e-5;
  interpolatePointWeighted = function(t, wp) {
    var cur, i, old, res, x0, x1, y0, y1, _i, _len, _ref, _ref1;
    if (wp.length <= 1) {
      return wp[0].p;
    } else {
      cur = 0;
      res = wp.length - 1;
      for (i = _i = 0, _len = wp.length; _i < _len; i = ++_i) {
        cur = wp[i].t;
        if (t < cur - eps) {
          if (i === 0) {
            return wp[0].p;
          }
          res = i - 1;
          t = (t - old) / (cur - old);
          break;
        }
        old = cur;
      }
      if (res + 1 >= wp.length) {
        return wp[res].p;
      } else {
        _ref = wp[res].p, x0 = _ref.x, y0 = _ref.y;
        _ref1 = wp[res + 1].p, x1 = _ref1.x, y1 = _ref1.y;
        return pt(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t);
      }
    }
  };
  return {
    Point: Point,
    distance: distance,
    distance2: distance2,
    raycast: raycast,
    PointSet: PointSet,
    interpolatePoint: interpolatePoint,
    interpolatePointWeighted: interpolatePointWeighted
  };
});
