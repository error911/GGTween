# GGTween

�========================================�
� Free license: CC BY Murnik Roman       �
� ��������� ��������: CC BY Murnik Roman �
�========================================�
� Tested in Unity 2019.2.X +	         �
L========================================-


�=====================�
� Support types       �
� �������������� ���� �
L=====================-
- Generic (any type of the following)
- Float
- Int
- Vector2
- Vector3
- Vector4
- Color
- Quaternion

�======================�
� Usage example        �
� ������ ������������� �
L======================-
		
		using GGTools.GGTween;
		void Sample_1()
		{
				var startPos = new Vector3(0,0,0);
				var endPos = new Vector3(3,6,0);
				Tween.TweenVector3((pos) => transform.position = pos, startPos, endPos, 1);
		}

�=====================================�
� Animation Interruption Example      �
� ������ ���������� �������� �������� �
L=====================================-
  
		void Sample_2()
		{
				int t = Tween.TweenFloat((x)=>Debug.Log(x), 0.0f, 1.0f, 5);
				Tween.EndTween(t);
		}