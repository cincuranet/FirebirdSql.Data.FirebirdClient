using System.Collections.Generic;
using System.Text;

namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public class EvaluationContext
	{
		private readonly StringBuilder _outputBuf = new StringBuilder();
		private int _currentIndex = 0;

		public EvaluationContext(string input, List<string> namedParameters)
		{
			Input = input;
			NamedParameters = namedParameters;
		}

		public void Output(char charachter)
		{
			_outputBuf.Append(charachter);
		}

		public bool HasTokensToProcess()
		{
			return _currentIndex < Input.Length;
		}

		public void NextToken()
		{
			_currentIndex++;
		}

		public void MoveTo(int i)
		{
			_currentIndex = i;
		}

		public List<string> NamedParameters { get; private set; }
		public string Input { get; private set; }
		public char CurrentToken { get { return _currentIndex < Input.Length ? Input[_currentIndex] : '\0'; } }
		public int CurrentIndex { get { return _currentIndex; } }

		public string Result
		{
			get { return _outputBuf.ToString(); }
		}
	}
}
