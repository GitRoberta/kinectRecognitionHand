   M S B A M L   `   `   `  ����J  FRecognitionHand, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null1clr-namespace:RecognitionHandRecognitionHand  5   	        RecognitionHand.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   /localclr-namespace:RecognitionHand     5   	     � Title$  
MainWindow��5   	   $	��500��6   $	��900��6)   .��5	      � .+�5
   
   �� -   5         ��Name$ labelBGR��5
      $��Color��6"   $
��Left=�62   $	��Top=�6O   �� 5   
   -   5         ��Name$ imageBGR��5      $
��Left=�6"   $	��300��6=   $��Center=�6K   $	��300��6f   �� 5   
   -   5        $ labelConvexHull��5      $��
ConvexHull��6)   $��Right=�6>   $	��Top=�6[   �� 5   
   -   5        $ imageConvexHull��5      $��Right=�6)   $	��300��6E   $��Center=�6R   $	��300��6m   �� 5   
   -   5        $ label��5      $��HSV Min Parameters��6   $
��Left=�6<   $��
356,25,0,0q�6W   $	��Top=�6k   �� 5   
   -   5         ��Name$ hsv_hue_min_slider��5       � Minimum$ 0Q�6-    � Maximum$ 10Q�69   $
��Left=�6F   $��
425,67,0,0q�6a   $	��Top=�6u   $	��110��6�   6�   �� 5   
   -   5        $ label1��5      $	��Hue��6    $
��Left=�6.   $��
356,63,0,0q�6I   $	��Top=�6]   �� 5   
   -   5        $ label2��5      $��
Saturation��6    $
��Left=�65   $��
355,88,0,0q�6P   $	��Top=�6d   �� 5   
   -	   5        $ hsv_min_sat_slider��5      $ 0Q�6-   $ 10Q�69   $
��Left=�6F   $��
425,92,0,0q�6a   $	��Top=�6u   $	��109��6�   6�   �� 5   
   -
   5        $ label3��5      $��Value��6    $
��Left=�60   $��356,112,0,0q�6K   $	��Top=�6`    J� RenderTransformOrigin$ -0.125,0.423&�6x   �� 5   
   -   5        $ hsv_min_value_slider��5   *   $ 0Q�6   $ 10Q�6   $
��Left=�6H   $��425,116,0,0q�6c   $	��Top=�6x   $	��109��6�   6�   �� 5   
   -   5        $ label4��5      $��HSV Max Parameters��6    $
��Left=�6=   $��356,184,0,0q�6X   $	��Top=�6m   �� 5   
   -   5        $ label5��5      $	��Hue��6    $
��Left=�6.   $��356,213,0,0q�6I   $	��Top=�6^   $ 0.375,0.462&�6v   �� 5   
   -   5        $ label6��5      $��
Saturation��6    $
��Left=�65   $��356,241,0,0q�6P   $	��Top=�6e   �� 5   
   -   5        $ label7��5      $��Value��6    $
��Left=�60   $��356,270,0,0q�6K   $	��Top=�6`   �� 5   
   -   5        $ hsv_max_hue_slider��5      $ 11Q�6-   $	 255Q�6:   $
��Left=�6H   $��424,217,0,0q�6c   $	��Top=�6x   $	��104��6�   6�   �� 5   
   -   5        $ hsv_max_saturation_slider��5   ,   $ 11Q�6   $	 255Q�6   $
��Left=�6O   $��424,245,0,0q�6j   $	��Top=�6   $ 0.667,0.111&�6�   $	��105��6�   6�   �� 5   
   -   5        $ hsv_max_value_slider��5   ,   $ 11Q�6   $	 255Q�6   $
��Left=�6J   $��424,274,0,0q�6e   $	��Top=�6z   $	��104��6�   6�   �� 5   
   -   5        $ label8��5      $��
Actual HSV��6    $
��Left=�65   $��356,332,0,0q�6P   $	��Top=�6e   �� 5   
   -   5        $ Actual_HSV_H��5      $��0��6&   $
��Left=�62   $��406,380,0,0q�6M   $	��Top=�6b   $��69��6z   �� 5   
   -   5        $ label9��5      $��H��6    $
��Left=�6,   $��355,380,0,0q�6G   $	��Top=�6\   �� 5   
   -   5        $ label10��5      $��S��6!   $
��Left=�6-   $��356,406,0,0q�6H   $	��Top=�6]   $ 0.184,0.269&�6u   �� 5    
   -   5        $ label11��5       $��V��6!   $
��Left=�6-   $��356,431,0,0q�6H   $	��Top=�6]   $ 1.353,0.462&�6u   �� 5!   
   -   5        $ ACTUAL_HSV_S��5!      $��0��6&   $
��Left=�62   $��406,406,0,0q�6M   $	��Top=�6b   $ 0.211,0.231&�6z   �� 5"   
   -   5        $ ACTUAL_HSV_V��5"      $��0��6&   $
��Left=�62   $��406,432,0,0q�6M   $	��Top=�6b   $ 0.211,-1.667&�6z   �� 5#   
   -   5        $ 	MIN_HSV_H��5#      $��0��6#   $
��Left=�6/   $��
542,62,0,0q�6J   $	��Top=�6^   $ 0.816,0.192&�6v   �� 5$   
   -   5        $ 	MIN_HSV_V��5$      $��0��6#   $
��Left=�6/   $��543,108,0,0q�6J   $	��Top=�6_   �� 5%   
   -   5        $ 	MAX_HSV_H��5%      $��0��6#   $
��Left=�6/   $��533,211,0,0q�6J   $	��Top=�6_   �� 5&   
   -   5        $ 	MAX_HSV_V��5&      $��0��6#   $
��Left=�6/   $��534,269,0,0q�6J   $	��Top=�6_   5+      5,      