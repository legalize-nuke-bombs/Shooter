using UnityEngine;

using System;
using System.Collections.Generic;

namespace Den.Tools.Splines
{ 

	[System.Serializable]
	public class SplineSys : ICloneable
	{
		public Line[] lines = new Line[0];
		//public Junction[] junctions;

		public bool guiDrawNodes = true;
		public bool guiDrawSegments = true;
		public bool guiDrawDots;
		public int guiDotsCount = 10;
		public bool guiDotsEquidist;


		public SplineSys () { }
		public SplineSys (SplineSys src) 
		{ 
			CopyLinesFrom(src.lines); 

			guiDrawNodes = src.guiDrawNodes;
			guiDrawSegments = src.guiDrawSegments;
			guiDrawDots = src.guiDrawDots;
			guiDotsCount = src.guiDotsCount;
			guiDotsEquidist = src.guiDotsEquidist;
		}


		public object Clone () { return new SplineSys(this); }

		public Vector3 GetPoint ( (int l, int s, float p) loc ) => lines[loc.l].segments[loc.s].GetPoint(loc.p);
		public Vector3 GetPoint (int l, int s, float p) => lines[l].segments[s].GetPoint(p);

		public Vector3 GetDerivative (int l, int s, float p) => lines[l].segments[s].GetDerivative(p);
		public Vector3 GetDerivative ( (int l, int s, float p) loc ) => lines[loc.l].segments[loc.s].GetDerivative(loc.p);

		public void AddLine (Line line) 
			{ ArrayTools.Add(ref lines, line); }

		public Line AddLine (Vector3 start, Vector3 end)
		{
			Line line = new Line(start, end);
			ArrayTools.Add(ref lines, line);
			return line;
		}

		public void AddLines (Line[] otherLines) 
			{ ArrayTools.AddRange(ref lines, otherLines); }

		public void CopyLinesFrom (Line[] otherLines)
		{
			lines = new Line[otherLines.Length];
			for (int l=0; l<lines.Length; l++)
				lines[l] = new Line(otherLines[l]);
		}

		public void RemoveLine (int l)
			{ ArrayTools.RemoveAt(ref lines, l); }

		public static SplineSys CreateDefault ()
		{
			SplineSys spline = new SplineSys();
			spline.lines = new Line[] { new Line() };
			spline.lines[0].segments = new Segment[] { new Segment(new Vector3(0,0,0), new Vector3(1,0,0)) };
			return spline;
		}


		public void Update () 
		/// Called by GUI on every line change and by system on global change
		{ for (int l=0; l<lines.Length; l++) lines[l].Update(); }


		public void UpdateTangents () 
			{ for (int l=0; l<lines.Length; l++) lines[l].UpdateTangents(); }

		public void UpdateLength () 
			{ for (int l=0; l<lines.Length; l++) lines[l].UpdateLength(); }


		public float Length ()
		{
			float length = 0;
			for (int l=0; l<lines.Length; l++)
			{
				if (lines[l].segments.Length == 0)
					continue;

				lines[l].UpdateLength();
				length += lines[l].length;
			}

			return length;
		}


		public List<SplineSys> SplitIntoConnected (float margin)
		{
			List<SplineSys> systems = new List<SplineSys>();
			float marginSq = margin * margin;

			for (int l=0; l<lines.Length; l++)
			{
				Line line = new Line(lines[l]);
				if (line.segments.Length == 0)
					continue;

				List<int> connectedSystems = new List<int>();
				for (int s=0; s<systems.Count; s++)
					if (IsLineConnectedToSpline(line, systems[s], marginSq))
						connectedSystems.Add(s);

				if (connectedSystems.Count == 0)
				{
					SplineSys system = new SplineSys();
					system.AddLine(line);
					systems.Add(system);
				}
				else
				{
					int dstSystemNum = connectedSystems[0];
					SplineSys dstSystem = systems[dstSystemNum];
					dstSystem.AddLine(line);

					for (int i=connectedSystems.Count-1; i>0; i--)
					{
						int srcSystemNum = connectedSystems[i];
						dstSystem.AddLines(systems[srcSystemNum].lines);
						systems.RemoveAt(srcSystemNum);
					}
				}
			}

			return systems;
		}


		private static bool IsLineConnectedToSpline (Line line, SplineSys spline, float marginSq)
		{
			for (int l=0; l<spline.lines.Length; l++)
				if (AreLinesConnected(line, spline.lines[l], marginSq))
					return true;

			return false;
		}


		private static bool AreLinesConnected (Line line1, Line line2, float marginSq)
		{
			return EndpointsNearNodes(line1, line2, marginSq) ||
				EndpointsNearNodes(line2, line1, marginSq);
		}


		private static bool EndpointsNearNodes (Line endpointsLine, Line nodesLine, float marginSq)
		{
			if (endpointsLine.segments.Length == 0 || nodesLine.segments.Length == 0)
				return false;

			Vector3 start = endpointsLine.startPos;
			Vector3 end = endpointsLine.endPos;

			for (int n=0; n<nodesLine.NodesCount; n++)
			{
				Vector3 node = nodesLine.GetNodePos(n);
				if (DistanceSqXZ(start, node) <= marginSq)
					return true;
				if (DistanceSqXZ(end, node) <= marginSq)
					return true;
			}

			return false;
		}


		private static float DistanceSqXZ (Vector3 p1, Vector3 p2)
		{
			float deltaX = p1.x - p2.x;
			float deltaZ = p1.z - p2.z;
			return deltaX*deltaX + deltaZ*deltaZ;
		}


		public Vector3[][] GetAllPoints (float resPerUnit=0.1f, int minRes=3, int maxRes=20)
		/// evaluates each of the splines, returning the positions
		/// if lengthFactor==1 resolution is multiplied with segment length
		{
			Vector3[][] points = new Vector3[lines.Length][];

			for (int l=0; l<lines.Length; l++)
				points[l] = lines[l].GetAllPoints(resPerUnit, minRes, maxRes);

			return points;
		}


		public int NodesCount 
		{get{
			int nodesCount = 0;
			for (int l=0; l<lines.Length; l++)
				nodesCount += lines[l].NodesCount;
			return nodesCount;
		}}


		public (int l, int s, float p, float dist) GetClosest (Vector3 point, int initialApprox=10, int recursiveApprox=10) =>
			GetClosest(null, point, initialApprox, recursiveApprox);

		public (int l, int s, float p, float dist) GetClosest (Func<Vector3,Vector3,float> distanceFn, int initialApprox=10, int recursiveApprox=10) =>
			GetClosest(distanceFn, new Vector3(), initialApprox, recursiveApprox);

		public (int l, int s, float p, float dist) GetClosest (Func<Vector3,Vector3,float> distanceFn, Vector3 point, int initialApprox=10, int recursiveApprox=10)
		/// Returns the closest location and distance from point to spline sys
		/// If distFn defined using it instead of point
		{
			float minDist = float.MaxValue;
			int ml=0;  int ms=0;  float mp=0;

			for (int l=0; l<lines.Length; l++)
			{
				(int cl, int cs, float cp, float curDist) = lines[l].GetClosest(distanceFn, point, initialApprox, recursiveApprox);
				if (curDist < minDist)
				{
					minDist = curDist;
					ms=cs; mp=cp; ml=cl;
				}
			}

			return (ml, ms, mp, minDist);
		}

		public float Handness (Vector3 point)
		/// Determines wheter the point is on the left or on the right of a spline (from top view)
		/// returns either positiove (left) or negative (right) value;
		{
			(int l, int s, float p, float dist) = GetClosest(point);
			Vector3 closest = GetPoint(l,s,p);
			Vector3 tangent = GetDerivative(l,s,p);

			Vector2 lineStart = new Vector2(closest.x, closest.z);
			Vector2 lineEnd = new Vector2(closest.x+tangent.x, closest.z+tangent.z);
			return (new Vector2(point.x,point.z)).Handness(lineStart, lineEnd);
		}


		public void Optimize (float deviation)
		/// Removes those nodes that should not change the shape a lot
		/// Works with auto-tangents only
		{ for (int l=0; l<lines.Length; l++) lines[l].Optimize(deviation); }


		public void Subdivide (int num, bool equiDistance=false)
		/// Splits each segment in several shorter segments
		{ for (int l=0; l<lines.Length; l++) lines[l].Subdivide(num); }

		public void Relax (float blur, int iterations) 
		/// Moves nodes to make the spline smooth
		/// Works with auto-tangents only
		{ for (int l=0; l<lines.Length; l++) lines[l].Relax(blur, iterations); }

		public void CutByRect (Vector3 pos, Vector3 size) 
		/// Splits all segments so that each intersection with AABB rect has a node
		{ for (int l=0; l<lines.Length; l++) lines[l].CutByRect(pos, size); }

		public void RemoveOuterSegments (Vector3 pos, Vector3 size)
		/// Removes segments with center placed out of pos-size AABB rect
		{
			List<Line> newLines = new List<Line>();
			for (int l=0; l<lines.Length; l++)
				newLines.AddRange (lines[l].OuterSegmentsRemoved(pos, size));
			lines = newLines.ToArray();
		}

		public void Clamp (Vector3 pos, Vector3 size) 
		/// Splits all segments so they are within AABB
		{ 
			CutByRect(pos, size);
			RemoveOuterSegments(pos, size);
		}


		public void PushPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
		{
			for (int l=0; l<lines.Length; l++)
				lines[l].PushPoints(points, ranges, horizontalOnly, distFactor);
		}


		public void PushStartEnd (Vector3[] points, float[] ranges, bool horizontalOnly=true, float distFactor=1)
		{
			for (int l=0; l<lines.Length; l++)
				lines[l].PushStartEnd(points, ranges, horizontalOnly, distFactor);
		}

		public void SplitNearPoints (Vector3[] points, float[] ranges, bool horizontalOnly=true, float startEndProximityFactor=0, int maxIterations=10)
		{
			for (int l=0; l<lines.Length; l++)
				lines[l].SplitNearPoints(points, ranges, horizontalOnly, startEndProximityFactor, maxIterations);
		}

		public void WeldCloseLines (float threshold)
		{
			List<Line> linesList = new List<Line>();
			linesList.AddRange(lines);

			int i = 0;
			Repeat:
			i++;
			if (i == lines.Length+1)
				throw new Exception("WeldCloseLines reached maximum iterations");

			for (int l1=0; l1<linesList.Count; l1++)
				for (int l2=0; l2<l1; l2++)
				{
					Line line1 = linesList[l1];
					Line line2 = linesList[l2];

					if (!Line.AreCloseToWeld(line1,line2,threshold))
						continue;

					Line[] weldedLines = Line.WeldClose(new Line(line1), new Line(line2), threshold);

					if (weldedLines.Length==1) //ignoring welding two lines in one
						continue;
						
					if (weldedLines.Length==2  &&  weldedLines[0].segments.Length + weldedLines[1].segments.Length == line1.segments.Length + line2.segments.Length)
					//no change (two lines and summary nodes count has not been changed, although they might be swapped)
					//TODO: compare point by point?
					{
						linesList[l1] = weldedLines[0];
						linesList[l2] = weldedLines[1]; //assigning lines just in case they have minor change
						continue;
					}

					linesList.RemoveAt(l1); //l2 is always less l1,  l1 is always bigger l2
					linesList.RemoveAt(l2);
					linesList.AddRange(weldedLines);

					goto Repeat; //exiting nested loop and restarting
				}

			lines = linesList.ToArray();
		}
	}
}
