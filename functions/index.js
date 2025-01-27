const functions = require("firebase-functions");
const admin = require("firebase-admin");
const OpenAI = require("openai");
const pdf = require("pdf-parse");

let fetch;
(async () => {
  fetch = (await import("node-fetch")).default;
})();

admin.initializeApp();

const openai = new OpenAI({
  apiKey: functions.config().openai.apikey,
});

function safeJSONParse(str) {
  try {
    return JSON.parse(str);
  } catch (error) {
    console.error("Error parsing JSON:", error);
    console.log("Problematic string:", str);
    return null;
  }
}

function validateQuestions(questions) {
  if (!Array.isArray(questions)) {
    throw new Error("Questions should be an array");
  }
  if (questions.length === 0) {
    throw new Error("No questions generated");
  }
  // Add more specific validations as needed
}

function validateAnswers(answers) {
  if (!Array.isArray(answers)) {
    throw new Error("Answers should be an array");
  }
  if (answers.length === 0) {
    throw new Error("No answers generated");
  }
  // Add more specific validations as needed
}

async function generateQuestions(text, numQuestions = 20, difficulty = "easy") {
  let prompt = "";
  switch (difficulty) {
    case "easy":
      prompt = `
        Generate a JSON array of ${numQuestions} true or false questions based on key terms or important facts from the text.
        Each question should be presented in the following format:
        
        [
          "1. True or False: The statement of fact or key term?",
          "2. True or False: Another statement?"
        ]
        
        The questions should be answerable with either 'True' or 'False' and directly related to the provided text:
        ${text}
      `;
      break;
    case "medium":
      prompt = `
        Generate a JSON array of ${numQuestions} multiple-choice questions based on key terms or important facts from the text.
        Each question should be formatted like this, with the answer choices appearing on the next line:

        [
          "1. What is the question? \\n a.) Option1\\n  b.) Option2\\n  c.) Option3\\n  d.) Option4",
          "2. Another question? \\n a.) Option1\\n  b.) Option2\\n  c.) Option3\\n  d.) Option4"
        ]
    
        Avoid open-ended questions, explanations, or questions that require listing multiple items. 
        All questions must be clear, concise, and focus on identification. 
        The questions should always describe or define a single key term, concept, or fact from the text. 
        Ensure that each question has four answer choices (a, b, c, d) and that both the questions and the choices are concise, clear, and answerable based on the provided text:
        ${text}
      `;
      break;
    case "hard":
      prompt = `
          Generate a JSON array of ${numQuestions} identification questions based on key terms or important facts from the text. 
          Each question should be structured as a definition or description of a specific term, concept, name, date, or fact that can be answered with a single word or a very short phrase (no more than 3 words). 
          For example, the question should follow this structure: 
          'What is defined as [description of the term]?' or 'What is the term for [definition]?'. 

          The answer to each question must be a specific term or phrase directly extracted from the text. 

          Format the output like this:
          [
            "1. What is defined as the creation of environments, scenarios, or missions in an electronic game?",
            "2. What is the term for the person responsible for directing the game’s overall design?"
          ]

          Ensure that each question is based on and directly relevant to the content of the following text:
          ${text}

          Avoid open-ended questions, explanations, or questions that require listing multiple items. 
          All questions must be clear, concise, and focus on identification. 
          The questions should always describe or define a single key term, concept, or fact from the text.
      `;
      break;
    default:
      throw new Error("Invalid difficulty level specified");
  }

  const chatCompletion = await openai.chat.completions.create({
    model: "gpt-3.5-turbo",
    messages: [
      { role: "system", content: "You are a helpful assistant." },
      {
        role: "user",
        content: `Please generate questions as follows: ${prompt}`,
      },
    ],
    max_tokens: 4000,
    temperature: 0.7,
  });

  const content = chatCompletion.choices[0].message.content.trim();
  functions.logger.info("Raw OpenAI API Response for questions:", content);

  let parsedContent;
  try {
    // First, try to parse the content as is
    parsedContent = JSON.parse(content);
  } catch (error) {
    functions.logger.warn("Failed to parse OpenAI response directly for questions. Attempting to extract JSON.");
    
    // If direct parsing fails, try to extract JSON from the content
    const jsonMatch = content.match(/\[[\s\S]*\]/);
    if (jsonMatch) {
      try {
        parsedContent = JSON.parse(jsonMatch[0]);
      } catch (innerError) {
        functions.logger.error("Failed to extract and parse JSON from OpenAI response for questions.", innerError);
      }
    }
    
    if (!parsedContent) {
      throw new Error(`Failed to parse OpenAI response as JSON for questions. Raw content: ${content}`);
    }
  }

  validateQuestions(parsedContent);
  
  // Ensure we're returning an array of strings
  return parsedContent.map(q => q.toString().trim());
  functions.logger.info("ParsedContent", parsedContent);
}


async function generateAnswers(text, questions, difficulty = "easy") {
  let prompt = "";
  switch (difficulty) {
    case "easy":
      prompt = `
        Based on the following text and questions, provide a JSON array of answers in this format:

        [
          "1. True",
          "2. False",
          "3. True"
        ]

        The answers should correspond directly to the questions listed below:

        This is the text where you have to base your answer:

        Text:
        ${text}

        Questions:
        ${questions.join('\n')}
      `;
      break;
    case "medium":
      prompt = `
        Based on the following text and questions, provide a JSON array of correct answers in this format:

        [
          "1. a",
          "2. c",
          "3. b"
        ]
        This is the text where you have to base your answer:

        Text:
        ${text}

        The answers should correspond directly to the multiple-choice questions listed below:

        Questions:
        ${questions.join('\n')}
      `;
      break;
    case "hard":
      prompt = `
          Based on the following text and questions, provide a JSON array of brief, specific answers in this format:

          [
            "1. CorrectTerm",
            "2. AnotherCorrectTerm"
          ]

          Each answer should consist of a single word or a very short phrase (no more than 3 words) and correspond directly to the identification questions below. 
          The answers must be directly extracted from the text without modification or interpretation. Do not generate new terms or provide any additional explanations.

          This is the text where you have to base your answer:
          Text:
          ${text}

          Questions:
          ${questions.join('\n')}
      `;  
      break;
    default:
      throw new Error("Invalid difficulty level specified");
  }

  const chatCompletion = await openai.chat.completions.create({
    model: "gpt-3.5-turbo",
    messages: [
      { role: "system", content: "You are a helpful assistant." },
      {
        role: "user",
        content: `Please generate answers as follows: ${prompt}`,
      },
    ],
    max_tokens: 1000,
  });

  const content = chatCompletion.choices[0].message.content.trim();
  functions.logger.info("Raw OpenAI API Response for answers:", content);

  let parsedContent;
  try {
    // First, try to parse the content as is
    parsedContent = JSON.parse(content);
  } catch (error) {
    functions.logger.warn("Failed to parse OpenAI response directly for answers. Attempting to extract JSON.");
    
    // If direct parsing fails, try to extract JSON from the content
    const jsonMatch = content.match(/\[[\s\S]*\]/);
    if (jsonMatch) {
      try {
        parsedContent = JSON.parse(jsonMatch[0]);
      } catch (innerError) {
        functions.logger.error("Failed to extract and parse JSON from OpenAI response for answers.", innerError);
      }
    }
    
    if (!parsedContent) {
      throw new Error(`Failed to parse OpenAI response as JSON for answers. Raw content: ${content}`);
    }
  }

  validateAnswers(parsedContent);
  
  // Ensure we're returning an array of strings
  return parsedContent.map(a => a.toString().trim());
  functions.logger.info("ParsedContent", parsedContent);
}

exports.generateQuestionsAndAnswersFromPdf = functions.https.onRequest(
  async (req, res) => {
    functions.logger.info("Generating questions and answers from PDF", req.body);

    res.set("Access-Control-Allow-Origin", "*");
    res.set("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    res.set("Access-Control-Allow-Headers", "Content-Type");

    if (req.method === "OPTIONS") {
      return res.status(204).send("");
    }

    const { pdfUrl, numQuestions = 20, difficulty = "easy" } = req.body;

    if (!pdfUrl) {
      return res
        .status(400)
        .send({ error: "Invalid request data. Ensure pdfUrl is provided." });
    }

    try {
      const response = await fetch(pdfUrl);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const pdfBuffer = await response.buffer();
      const pdfData = await pdf(pdfBuffer);
      const extractedText = pdfData.text;
    
      functions.logger.info("Extracted text from PDF:", extractedText);
    
      const questions = await generateQuestions(
        extractedText,
        numQuestions,
        difficulty
      );
      
      functions.logger.info("Generated questions array:", questions);
    
      const answers = await generateAnswers(
        extractedText,
        questions,
        difficulty
      );
      functions.logger.info("Generated answers array:", answers);
    
      // Remove numbering from questions and answers
      const cleanQuestions = questions.map((q, index) => {
        return q.replace(/^\d+\.\s*/, "").trim(); // Clean each question
      });

      const cleanAnswers = answers.map((a, index) => {
        return a.replace(/^\d+\.\s*/, "").trim(); // Clean each question
      });
    
      // const cleanAnswers = answers.map((a) =>
      //   a.replace(/^\d+\.\s*/, "").trim()
      // );
    
      // Log cleaned questions and answers for debugging
      functions.logger.info("Cleaned questions:", cleanQuestions);
      functions.logger.info("Cleaned answers:", cleanAnswers);
    
      // Ensure we're sending back arrays
      return res.status(200).send({
        questions: cleanQuestions,  // Each question is separate
        answers: cleanAnswers       // Each answer is separate as well
      });
    } catch (error) {
      console.error(
        "Error processing PDF or generating questions and answers:",
        error
      );
      return res.status(500).send({
        error: "Error generating questions and answers from PDF",
        details: error.message,
        stack: error.stack,
        rawContent: error.message.includes("Raw content:") ? error.message.split("Raw content:")[1].trim() : "Not available"
      });
    }
  }    
);