import os
import tempfile
import firebase_admin
from firebase_admin import credentials, firestore, storage
from google.cloud import storage as gcs
from PyPDF2 import PdfReader
import openai
import requests

# Initialize Firebase
cred = credentials.ApplicationDefault()
firebase_admin.initialize_app(cred, {
    'storageBucket': os.getenv('GCLOUD_PROJECT') + '.appspot.com'
})

db = firestore.client()
bucket = storage.bucket()

# Function to extract text from a PDF file
def extract_text_from_pdf(pdf_path):
    reader = PdfReader(pdf_path)
    text = ""
    for page in reader.pages:
        text += page.extract_text()
    return text

# Function to generate questions using GPT-3.5 Turbo API
def generate_questions(text, num_questions=10):
    api_key = "sk-proj-VeNnzPsgYz8sEAbzEaubT3BlbkFJCWK50fCbkv00U1CTa528"
    openai.api_key = api_key

    prompt = (
        "Generate {} relevant identification questions that are exam-like based on the important terms in the text. Don't create \"Why\" and \"How\" Questions. "
        "Follow this format: 1. (Question)?\n\n{}".format(num_questions, text)
    )

    response = openai.ChatCompletion.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        max_tokens=500,
        temperature=0.7
    )

    questions = response['choices'][0]['message']['content']
    return questions.strip().split("\n")

# Function to generate answers for the questions using GPT-3.5 Turbo API
def generate_answers(text, questions):
    api_key = "YOUR_OPENAI_API_KEY"
    openai.api_key = api_key

    prompt = (
        "Here are some questions based on the following text:\n\n{}\n\n"
        "Provide the answers to these questions. Limit the answers to 2 phrases only, "
        "don't use sentences for answers and no commas but don't remove all the special characters."
        " Use this format: 1. Answer\n\nQuestions:\n{}".format(text, "\n".join(questions))
    )

    response = openai.ChatCompletion.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": "You are a helpful assistant."},
            {"role": "user", "content": prompt}
        ],
        max_tokens=500
    )

    answers = response['choices'][0]['message']['content']
    return answers.strip().split("\n")

# Firebase Function to process the PDF and generate questions and answers
def process_pdf(event, context):
    # Get the file from the event
    file_path = event['name']
    file_name = os.path.basename(file_path)

    # Download the file to a temporary location
    _, temp_local_filename = tempfile.mkstemp()
    bucket = gcs.Client().bucket(os.getenv('GCLOUD_PROJECT') + '.appspot.com')
    blob = bucket.blob(file_path)
    blob.download_to_filename(temp_local_filename)

    # Extract text from PDF
    text = extract_text_from_pdf(temp_local_filename)

    # Generate questions and answers
    questions = generate_questions(text)
    answers = generate_answers(text, questions)

    # Store results in Firestore
    doc_ref = db.collection('pdfResults').document(file_name)
    doc_ref.set({
        'questions': questions,
        'answers': answers,
        'createdAt': firestore.SERVER_TIMESTAMP
    })

    # Clean up temporary file
    os.remove(temp_local_filename)

    print(f'PDF processed: {file_name}')

# Set up a Firebase Cloud Function trigger
def main(request):
    event = request.get_json()
    process_pdf(event, None)
    return 'PDF processing initiated.', 200
