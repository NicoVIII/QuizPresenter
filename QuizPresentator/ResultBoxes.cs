﻿using System;
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;

namespace QuizPresentator {
	public class ResultBoxes : HBox {
		VBox[] lifelineBoxes;
		ResultBox[] boxes;
		ImageView[,] lifelines;

		public ResultBoxes(Quiz quiz) {
			Party[] parties = quiz.Parties;
			boxes = new ResultBox[parties.Length];
			lifelineBoxes = new VBox[parties.Length];
			// Initialise boxes
			lifelines = new ImageView[parties.Length, quiz.Lifelines.Length];
			for (int i = 0; i < parties.Length; i++) {
				lifelineBoxes[i] = new VBox();

				/*for (int j = 0; j < nrOfLifelines; j++) {
					Lifeline ll = quiz.Parties[i].Lifelines[j];

					//Determine image
					Image image = null;
					switch (ll.Name) {
						case "50-50":
							image = Image.FromFile("images/lifelines/50-50.png").WithSize(85, 85);
							break;
						case "telephone":
							image = Image.FromFile("images/lifelines/telephone_lifeline.png").WithSize(85, 85);
							break;
						case "audience":
							image = Image.FromFile("images/lifelines/ask_the_audience_lifeline.png").WithSize(85, 85);
							break;
						case "additional":
							image = Image.FromFile("images/lifelines/additional_lifeline.png").WithSize(85, 85);
							break;
					}

					ImageView iv = new ImageView(image);
					lifelineBoxes[i].PackStart(iv);
					lifelines[i, j] = iv;
				}*/
				PackStart(lifelineBoxes[i]);

				ResultBox box = new ResultBox(parties[i]);
				boxes[i] = box;
				PackStart(box);
			}
		}

		public void Update(Quiz quiz) {
			Party[] parties = quiz.Parties;
			for (int i = 0; i < parties.Length; i++) {
				boxes[i].Update(parties[i]);
			}
		}
	}
}