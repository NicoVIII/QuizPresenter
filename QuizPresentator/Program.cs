﻿using QuizPresentator;
using System;
using System.IO;
using Xwt;
using Xwt.Drawing;

namespace QuizPresentator {
	class ImageCanvas : Canvas {
		private Image image;
		private Rectangle rect;
		private bool boundsChanged = false;

		public ImageCanvas(Image image) {
			this.image = image;

			BoundsChanged += (sender, e) => {
				if (this.image != null) {
					Size boxSize = new Size(Size.Width - 2 * Parameter.BorderRadius, Size.Height - 2 * Parameter.BorderRadius);
					Size imageSize = this.image.Size;
					if (imageSize.Width / imageSize.Height < boxSize.Width / boxSize.Height) {
						// Image is higher
						int width = (int)(imageSize.Width * (boxSize.Height / imageSize.Height));
						rect = new Rectangle((boxSize.Width - width) / 2, Parameter.BorderRadius, width, boxSize.Height);
					}
					else if (imageSize.Width / imageSize.Height > boxSize.Width / boxSize.Height) {
						int height = (int)(imageSize.Height * (boxSize.Width / imageSize.Width));
						rect = new Rectangle(Parameter.BorderRadius, (boxSize.Height - height) / 2, boxSize.Width, height);
					}
					else {
						rect = new Rectangle(Parameter.BorderRadius, Parameter.BorderRadius, boxSize.Width, boxSize.Height);
					}
					QueueDraw();
				}
			};
		}

		public void Update(int questionIndex) {
			string fileName = "images/" + questionIndex;
			if (File.Exists(fileName + ".jpg")) {
				image = Image.FromFile(fileName + ".jpg");
			} else if (File.Exists(fileName + ".png")) {
				image = Image.FromFile(fileName + ".png");
			} else {
				image = null;
			}

			boundsChanged = true;
			QueueDraw();
		}

		protected override void OnDraw(Context ctx, Rectangle dirtyRect) {
			if (boundsChanged) {
				OnBoundsChanged();
				boundsChanged = false;
			}

			ctx.MoveTo(Parameter.BorderRadius, 0);
			ctx.LineTo(0, Parameter.BorderRadius);
			ctx.LineTo(0, Size.Height - Parameter.BorderRadius);
			ctx.LineTo(Parameter.BorderRadius, Size.Height);
			ctx.LineTo(Size.Width - Parameter.BorderRadius, Size.Height);
			ctx.LineTo(Size.Width, Size.Height - Parameter.BorderRadius);
			ctx.LineTo(Size.Width, Parameter.BorderRadius);
			ctx.LineTo(Size.Width - Parameter.BorderRadius, 0);
			ctx.ClosePath();
			ctx.SetColor(Color.FromHsl(0, 0, 0.77));
			ctx.Fill();

			if (image != null)
				ctx.DrawImage(image, rect);
		}
	}

	class MainClass {
		static Quiz quiz;
		static QuestionBox questionBox;
		static ResultBoxes resultBoxes;
		static State state = State.START;
		static AnswerIndex choosenAnswer;

		static int question = 0;

		enum State {
			// TODO check if this state is necessary or if it could be removed. Result would be the new starting state
			START,
			WAIT_FOR_ANSWER,
			LOGGED_IN,
			RESULT,
			END
		}

		/*private static void useLifeline(int index) {
			quiz = quiz.UseLifeline(quiz.ActiveParty, index);
			// 50-50
			if (quiz.Parties[quiz.activeParty].Lifelines[index].Type == Logic.LLType.FiftyFiftyLL) {
				questionBox.FiftyFifty(quiz);
			}
			resultBoxes.Update(quiz);
		}//*/

		[STAThread]
		static void Main(string[] args)
		{
			// Initialize Quiz
			quiz = Quiz.FromFile(Parameter.QuizFilePath);

			// Init Gui
			#if WIN
			Application.Initialize(ToolkitType.Wpf);
			var mainWindow = new Window()
			{
				Title = "Quizpresentation",

				Decorated = false,
				FullScreen = true
			};
			#elif MAC
			Application.Initialize(ToolkitType.Gtk);
			var mainWindow = new Window()
			{
				Title = "Quizpresentation",

				Width = 800,
				Height = 600
			};
			#else
			Application.Initialize(ToolkitType.Gtk);
			var mainWindow = new Window()
			{
				Title = "Quizpresentation",

				Decorated = false,
				FullScreen = true
			};
			#endif

			// Initialize question screen
			Box outerContainer = new VBox();
			mainWindow.Content = outerContainer;
			outerContainer.HorizontalPlacement = WidgetPlacement.Fill;

			// Upper half
			Box upperHalf = new HBox();
			outerContainer.PackStart(upperHalf, true, true);
			upperHalf.VerticalPlacement = WidgetPlacement.Fill;

			// Imageview
			ImageCanvas imageCanvas = new ImageCanvas(null);
			upperHalf.PackStart(imageCanvas, true);

			// ResultBoxes
			resultBoxes = new ResultBoxes(quiz);
			upperHalf.PackEnd(resultBoxes);

			// Lower half
			questionBox = new QuestionBox();
			outerContainer.PackEnd(questionBox);

			// Initialize keyboard listener
			mainWindow.Content.KeyPressed += (sender, e) => {
				// Enable Escape Key
				if (e.Key == Xwt.Key.Escape) {
					Application.Exit();
				}

				switch (state) {
					case State.START:
						// Does the same as result atm
						goto case State.RESULT;
					case State.WAIT_FOR_ANSWER:
						switch (e.Key) {
							case Key.K1:
							case Key.NumPad1:
							case Key.F1:
								choosenAnswer = AnswerIndex.A;
								questionBox.LogIn(choosenAnswer);
								state = State.LOGGED_IN;
								break;
							case Key.K2:
							case Key.NumPad2:
							case Key.F2:
								choosenAnswer = AnswerIndex.B;
								questionBox.LogIn(choosenAnswer);
								state = State.LOGGED_IN;
								break;
							case Key.K3:
							case Key.NumPad3:
							case Key.F3:
								choosenAnswer = AnswerIndex.C;
								questionBox.LogIn(choosenAnswer);
								state = State.LOGGED_IN;
								break;
							case Key.K4:
							case Key.NumPad4:
							case Key.F4:
								choosenAnswer = AnswerIndex.D;
								questionBox.LogIn(choosenAnswer);
								state = State.LOGGED_IN;
								break;
							/*case Key.q:
							case Key.Q:
								useLifeline(0);
								break;
							case Key.w:
							case Key.W:
								useLifeline(1);
								break;
							case Key.e:
							case Key.E:
								useLifeline(2);
								break;
							case Key.r:
							case Key.R:
								useLifeline(3);
								break;
							case Key.t:
							case Key.T:
								useLifeline(4);
								break;*/
						}
						break;
					case State.LOGGED_IN:
						// Update
						if (e.Key.Equals(Xwt.Key.Space)) {
							questionBox.ShowResult(choosenAnswer, quiz.CurrentQuestion.CheckAnswer(choosenAnswer));
							quiz = quiz.ChooseAnswer(choosenAnswer);
							state = State.RESULT;
						}
						// Enable to change logged in answer
						goto case State.WAIT_FOR_ANSWER;
					case State.RESULT:
						// Update
						if (e.Key.Equals(Xwt.Key.Space)) {
							resultBoxes.Update(quiz);
							questionBox.Update(quiz);
							imageCanvas.Update(++question);
							if (quiz.Ended) {
								state = State.END;
								questionBox.Hide();
							} else
								state = State.WAIT_FOR_ANSWER;
						}
						break;
					case State.END:
						// Nothing to do
						break;
				}
			};
			mainWindow.Content.CanGetFocus = true;
			mainWindow.Content.SetFocus();

			// Start Application
			mainWindow.Show();
			Application.Run();
			mainWindow.Dispose();
		}
	}
}