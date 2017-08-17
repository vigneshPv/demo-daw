namespace A1050055001a
{

	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	public static class DotFinder {
		
		private static bool m_initialized = false;
		private static int m_cameraWidth = 0;
		private static int m_cameraHeight = 0;
		private static int m_cThresh = 82;
		
		public static bool Initialize(int cameraWidth, int cameraHeight)
		{
			if(m_initialized)
			{
				return true;
			}
			
			if(cameraWidth <= 0 || cameraHeight <= 0)
			{
				return false;
			}
			
			m_cameraWidth = cameraWidth;
			m_cameraHeight = cameraHeight;
			
			m_initialized = true;
			
			return true;
		}

		public static Vector2[] FindPoints(Rect frameArea, int nbMaxPoints, Color32[] frame)
		{
			if(!m_initialized || frame == null)
			{
				return null;
			}
			
			if(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				frameArea.width = Mathf.Clamp(frameArea.width, 0f, 1f);
				frameArea.height = Mathf.Clamp(frameArea.height, 0f, 1f);
				frameArea.x = Mathf.Clamp(frameArea.x, 0f, 1f);
				frameArea.y = Mathf.Clamp(frameArea.y, 0f, 1f);
			}
			else
			{
				frameArea.width = Mathf.Clamp(frameArea.height, 0f, 1f);
				frameArea.height = Mathf.Clamp(frameArea.width, 0f, 1f);
				frameArea.x = Mathf.Clamp(frameArea.y, 0f, 1f);
				frameArea.y = Mathf.Clamp(frameArea.x, 0f, 1f);
			}
			
			if((frameArea.x + frameArea.width) > 1f)
			{
				frameArea.width = 1f-frameArea.x;
			}
			if((frameArea.y + frameArea.height) > 1f)
			{
				frameArea.height = 1f-frameArea.y;
			}
			
			if(frameArea.width <= 0f || frameArea.height <= 0f)
			{
				return null;
			}
			
			frameArea.x *= (float)m_cameraWidth;
			frameArea.y *= (float)m_cameraHeight;
			frameArea.width *= (float)m_cameraWidth;
			frameArea.height *= (float)m_cameraHeight;

			float screenRatio = Mathf.Max((float)Screen.width, (float)Screen.height)/Mathf.Min((float)Screen.width, (float)Screen.height);
			float cameraRatio = Mathf.Max((float)m_cameraWidth, (float)m_cameraHeight)/Mathf.Min((float)m_cameraWidth, (float)m_cameraHeight);
			float widthRatio = screenRatio/cameraRatio;
			float heightRatio = cameraRatio/screenRatio;

			byte[] grayscaleFrame = ConvertFrameAreaToGrayscale(frame, frameArea);

			Vector2[] pts = Manager.FastDetectCorners(grayscaleFrame, (int)frameArea.width, (int)frameArea.height, m_cThresh);
            if(pts != null)
			{
				int nbPts = Mathf.Min(pts.Length, nbMaxPoints);
				Vector2 newPos;
				for(int i=0; i<nbPts; ++i)
				{
					if(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
					{
						newPos.x = 1f-(((float)pts[i].y)+frameArea.y)/(float)m_cameraHeight;
						newPos.y = 1f-(((float)pts[i].x)+frameArea.x)/(float)m_cameraWidth;
					}
					else
					{
						newPos.x = (((float)pts[i].x)+frameArea.x)/(float)m_cameraWidth;
						newPos.y = 1f-(((float)pts[i].y)+frameArea.y)/(float)m_cameraHeight;
					}
					if(screenRatio > cameraRatio)
					{
						if(newPos.x > 0.5f)
							newPos.x = 0.5f+(newPos.x-0.5f)*widthRatio;
						else if(newPos.x < 0.5f)
							newPos.x = 0.5f-(0.5f-newPos.x)*widthRatio;
					}
					else if(screenRatio < cameraRatio)
					{
						if(newPos.y > 0.5f)
							newPos.y = 0.5f+(newPos.y-0.5f)*heightRatio;
						else if(newPos.y < 0.5f)
							newPos.y = 0.5f-(0.5f-newPos.y)*heightRatio;
					}
					pts[i] = newPos;
				}

                // Try to adjust the threshold so that we always have 100-300 dots around...
                if (pts.Length < 100)
                {
                    m_cThresh -= 3;
                } else if (pts.Length > 500)
                {
                    m_cThresh += 3;
                }
                
                m_cThresh = (int) Mathf.Clamp(m_cThresh, 1, 255); // Actual limit is 0 to 255
			}

			return pts;
		}
		
		public static byte[] ConvertFrameAreaToGrayscale(Color32[] frame, Rect frameArea)
		{
			if(frame == null)
			{
				return null;
			}
			
			int frameAreaLenght = (int)frameArea.width*(int)frameArea.height;
			byte[] grayFrame = new byte[frameAreaLenght];
			int pos;
			for(int i=0; i<frameAreaLenght; ++i)
			{
				pos = (int)((int)frameArea.x+i%(int)frameArea.width)+((int)frameArea.y+i/(int)frameArea.width)*m_cameraWidth;
				// Average method
				grayFrame[i] = (byte)(((int)frame[pos].r+(int)frame[pos].g+(int)frame[pos].b)/3);
			}
			return grayFrame;
		}
	}
}

