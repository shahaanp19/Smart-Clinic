console.log("Chatbot JS loaded");
document.addEventListener("DOMContentLoaded", function () {

    const input = document.getElementById("userQuestion");
    const askButton = document.getElementById("askAssistantButton");
    const responseContainer = document.getElementById("chatResponse");
    const quickQuestions = document.querySelectorAll(".quick-question");

    if (!input || !askButton || !responseContainer) {
        return;
    }

    const responseGroups = [

        // GREETINGS

        {
            keywords: [
                "hello",
                "hi",
                "hey",
                "good morning",
                "good afternoon",
                "good evening"
            ],
            response:
                "Hello! Welcome to Doringkloof Medical Centre. I can help you with appointments, clinic services, patient accounts, medical records and general healthcare information. What would you like to know?"
        },

        {
            keywords: [
                "who are you",
                "what are you",
                "what can you do",
                "help me"
            ],
            response:
                "I am the Doringkloof Medical Centre Virtual Health Assistant. I can answer common questions about appointments, services, accounts, medical records and general healthcare information. I cannot diagnose conditions, prescribe medication or replace advice from a qualified healthcare professional."
        },

        // APPOINTMENTS

        {
            keywords: [
                "book appointment",
                "make appointment",
                "schedule appointment",
                "book a doctor",
                "see a doctor"
            ],
            response:
                "To book an appointment, sign in to the Patient Portal and select the appointment booking option. Choose the healthcare professional, select an available date and time, and confirm the appointment. If you need assistance, reception can help you with the booking process."
        },

        {
            keywords: [
                "cancel appointment",
                "cancel my appointment"
            ],
            response:
                "You can cancel an appointment through the My Appointments section of the Patient Portal. Select the appointment you no longer need and follow the cancellation instructions."
        },

        {
            keywords: [
                "reschedule appointment",
                "change appointment",
                "move appointment"
            ],
            response:
                "You can reschedule an appointment through the My Appointments section of the Patient Portal. Select the appointment and choose another available date and time."
        },

        {
            keywords: [
                "upcoming appointment",
                "next appointment",
                "my appointments"
            ],
            response:
                "Your upcoming appointments can be viewed in the My Appointments section of the Patient Portal. The appointment details include the scheduled date, time and healthcare professional."
        },

        {
            keywords: [
                "appointment time",
                "when is my appointment"
            ],
            response:
                "You can check your appointment date and time by signing in to the Patient Portal and opening My Appointments."
        },

        {
            keywords: [
                "arrive early",
                "how early",
                "when should i arrive"
            ],
            response:
                "Patients are encouraged to arrive at least 15 minutes before their scheduled appointment so that there is enough time for reception and any required administrative checks."
        },

        {
            keywords: [
                "miss appointment",
                "missed appointment"
            ],
            response:
                "If you have missed an appointment, please contact reception as soon as possible. They can advise you about rescheduling and the clinic's appointment policy."
        },

        {
            keywords: [
                "choose doctor",
                "select doctor",
                "specific doctor"
            ],
            response:
                "Yes. During appointment booking, you can select an available healthcare professional according to the appointment options provided by the clinic."
        },

        // ACCOUNT

        {
            keywords: [
                "create account",
                "new account",
                "register",
                "sign up",
                "registration"
            ],
            response:
                "To create a patient account, select Register from the login page and provide the required information. Once registration is complete, you can use your account to access the Patient Portal."
        },

        {
            keywords: [
                "forgot password",
                "forgot my password",
                "reset password"
            ],
            response:
                "If you have forgotten your password, select Forgot Password on the login page and follow the password recovery instructions."
        },

        {
            keywords: [
                "change password",
                "update password"
            ],
            response:
                "You can change your password through the account settings available in the Patient Portal. Choose the password or security option and follow the instructions."
        },

        {
            keywords: [
                "update profile",
                "edit profile",
                "change my details",
                "personal information"
            ],
            response:
                "Your personal information can be managed through the Profile or Settings section of the Patient Portal, where available."
        },

        {
            keywords: [
                "login",
                "log in",
                "sign in",
                "cannot login",
                "can't login"
            ],
            response:
                "Use your registered email address and password on the login page. If you cannot sign in, check that your credentials are correct and that your account is active. If the problem continues, contact the clinic for assistance."
        },

        {
            keywords: [
                "secure",
                "security",
                "is my information safe",
                "is my data safe"
            ],
            response:
                "The Smart Clinic system uses authenticated access, role-based permissions and secure password hashing to protect patient and staff information. You should also keep your password private and sign out when using a shared device."
        },

        // MEDICAL RECORDS

        {
            keywords: [
                "medical records",
                "medical record",
                "health records",
                "health record"
            ],
            response:
                "If your account has access to medical records, you can view your available clinical information through the Medical Records section of the Patient Portal. Access is restricted according to your account permissions."
        },

        {
            keywords: [
                "view records",
                "see my records",
                "access records"
            ],
            response:
                "Sign in to the Patient Portal and open the Medical Records section to view the records available to your account."
        },

        {
            keywords: [
                "prescription",
                "prescriptions",
                "prescribed medicine"
            ],
            response:
                "Prescriptions can only be issued or approved by a qualified healthcare professional following an appropriate consultation. The Virtual Health Assistant cannot prescribe medication."
        },

        {
            keywords: [
                "repeat prescription",
                "repeat medication"
            ],
            response:
                "Repeat prescriptions need to be reviewed and approved by a qualified healthcare professional in accordance with the clinic's procedures."
        },

        // CLINIC INFORMATION

        {
            keywords: [
                "services",
                "what services",
                "services offered",
                "what do you offer"
            ],
            response:
                "Doringkloof Medical Centre provides a range of healthcare services, including Doctors, Dentistry, Dietetics, Orthotics & Prosthetics, Physiotherapy and Podiatry."
        },

        {
            keywords: [
                "doctor",
                "doctors",
                "medical doctor"
            ],
            response:
                "Our Doctors service provides general medical care and professional assessment for patients. Appointments can be booked through the Patient Portal."
        },

        {
            keywords: [
                "dentist",
                "dentistry",
                "dental"
            ],
            response:
                "Our Dentistry service focuses on oral health, including preventive and general dental care. Please contact the practice or book an appointment for dental concerns."
        },

        {
            keywords: [
                "dietitian",
                "dietetics",
                "nutrition",
                "nutritionist"
            ],
            response:
                "Our Dietetics service provides personalised nutrition guidance to support healthier eating habits, lifestyle choices and individual healthcare goals."
        },

        {
            keywords: [
                "orthotics",
                "prosthetics",
                "orthotic",
                "prosthetic"
            ],
            response:
                "Our Orthotics & Prosthetics service focuses on supporting mobility, physical function and independence through appropriate professional assessment and rehabilitation support."
        },

        {
            keywords: [
                "physiotherapy",
                "physio",
                "physical therapy"
            ],
            response:
                "Our Physiotherapy service supports movement, physical function and rehabilitation. Patients can contact the practice or book an appointment for professional assessment."
        },

        {
            keywords: [
                "podiatry",
                "podiatrist",
                "feet",
                "foot care"
            ],
            response:
                "Our Podiatry service focuses on the health and function of the feet, ankles and lower limbs. Professional assessment can help address concerns affecting comfort and mobility."
        },

        {
            keywords: [
                "location",
                "where is the clinic",
                "where are you",
                "address",
                "clinic address"
            ],
            response:
                "Doringkloof Medical Centre is located at Doringkloof Mall, 6 Aster Avenue, Centurion."
        },

        {
            keywords: [
                "operating hours",
                "opening hours",
                "when are you open"
            ],
            response:
                "Clinic operating hours can vary depending on the healthcare professional and service. Please contact reception for the most current operating hours."
        },

        {
            keywords: [
                "walk in",
                "walk-in",
                "without appointment"
            ],
            response:
                "Walk-in patients may be accommodated depending on availability. Booking an appointment in advance is recommended to help ensure that a healthcare professional is available."
        },

        {
            keywords: [
                "contact reception",
                "reception",
                "contact clinic"
            ],
            response:
                "Reception can assist with appointments, clinic information, account-related questions and general administrative enquiries. Please use the Contact page for the clinic's contact details."
        },

        // GENERAL HEALTH

        {
            keywords: [
                "headache",
                "head hurts",
                "head pain"
            ],
            response:
                "Headaches can have many causes, including dehydration, stress, lack of sleep, illness or other medical conditions. Rest, hydration and monitoring your symptoms may help with mild headaches. If the headache is severe, sudden, persistent or accompanied by other concerning symptoms, seek medical attention."
        },

        {
            keywords: [
                "flu",
                "flu symptoms",
                "influenza"
            ],
            response:
                "Common flu symptoms can include fever, cough, sore throat, fatigue, headache and body aches. Rest and adequate fluids may help with mild illness. If symptoms are severe, worsening or persistent, consult a healthcare professional."
        },

        {
            keywords: [
                "cough",
                "coughing"
            ],
            response:
                "A cough can occur with colds, flu, allergies and other conditions. Monitor your symptoms and seek medical advice if the cough is severe, persistent, worsening or associated with difficulty breathing or chest pain."
        },

        {
            keywords: [
                "fever",
                "high temperature"
            ],
            response:
                "A fever can occur when the body is responding to an infection or another condition. Stay hydrated and monitor your symptoms. If the fever is very high, persistent or accompanied by serious symptoms, seek medical attention."
        },

        {
            keywords: [
                "sore throat",
                "throat pain"
            ],
            response:
                "A sore throat can be caused by viral infections, bacterial infections or irritation. Fluids and rest may help with mild symptoms. Persistent, severe or worsening symptoms should be assessed by a healthcare professional."
        },

        {
            keywords: [
                "stomach ache",
                "stomach pain",
                "abdominal pain"
            ],
            response:
                "Stomach pain can have many causes, ranging from minor digestive problems to conditions requiring medical assessment. Severe, persistent or worsening abdominal pain should be evaluated by a healthcare professional."
        },

        {
            keywords: [
                "back pain",
                "backache"
            ],
            response:
                "Back pain can result from muscle strain, posture, injury or other conditions. Gentle activity and appropriate self-care may help mild discomfort, but severe, persistent or worsening pain should be medically assessed."
        },

        {
            keywords: [
                "toothache",
                "tooth pain"
            ],
            response:
                "Toothache can be caused by dental decay, infection, gum problems or other dental conditions. Please arrange an appointment with a dental professional, particularly if the pain is severe or persistent."
        },

        {
            keywords: [
                "dizzy",
                "dizziness"
            ],
            response:
                "Dizziness can have many causes, including dehydration, illness, medication effects or changes in blood pressure. Sit or lie down safely if you feel faint and seek medical advice if the dizziness is severe, recurrent or associated with other concerning symptoms."
        },

        {
            keywords: [
                "fatigue",
                "tired all the time",
                "very tired"
            ],
            response:
                "Fatigue can be related to poor sleep, stress, illness, nutrition or other medical conditions. If unexplained fatigue persists or interferes with your daily activities, consider arranging an appointment with a healthcare professional."
        },

        // EMERGENCIES

        {
            keywords: [
                "chest pain",
                "pain in chest"
            ],
            response:
                "⚠️ Chest pain can be a medical emergency. If you are experiencing severe, sudden or unexplained chest pain, especially with shortness of breath, sweating, nausea, weakness or pain spreading to the arm, jaw or back, seek emergency medical care immediately."
        },

        {
            keywords: [
                "difficulty breathing",
                "shortness of breath",
                "cant breathe",
                "can't breathe",
                "cannot breathe",
                "trouble breathing"
            ],
            response:
                "⚠️ Difficulty breathing can be a medical emergency. Seek immediate emergency medical attention, especially if breathing difficulty is severe, sudden or worsening."
        },

        {
            keywords: [
                "stroke",
                "face drooping",
                "slurred speech",
                "weakness one side"
            ],
            response:
                "⚠️ Possible stroke symptoms require urgent emergency care. Signs can include sudden facial drooping, weakness or numbness on one side, difficulty speaking, confusion or sudden loss of balance. Contact emergency services immediately."
        },

        {
            keywords: [
                "heavy bleeding",
                "bleeding heavily",
                "severe bleeding"
            ],
            response:
                "⚠️ Heavy or uncontrolled bleeding requires immediate medical attention. Apply firm pressure to the wound if appropriate and contact emergency services or go to the nearest emergency department."
        },

        {
            keywords: [
                "heart attack",
                "heart attack symptoms"
            ],
            response:
                "⚠️ A possible heart attack is a medical emergency. Symptoms may include chest pressure or pain, shortness of breath, sweating, nausea or pain spreading to the arm, jaw, neck or back. Contact emergency services immediately."
        },

        {
            keywords: [
                "broken leg",
                "broken bone",
                "fracture"
            ],
            response:
                "⚠️ A suspected fracture requires medical assessment. Avoid putting unnecessary weight on the injured area and seek urgent medical care, particularly if there is severe pain, deformity, numbness or an open wound."
        }
    ];


    function findResponse(question) {

        for (const group of responseGroups) {

            for (const keyword of group.keywords) {

                if (question.includes(keyword)) {
                    return group.response;
                }
            }
        }

        return null;
    }


    function getResponse() {

        const question = input.value.trim().toLowerCase();

        if (!question) {

            responseContainer.innerHTML =
                `<div class="alert alert-light border mt-3" role="alert">
                    Please enter a question before asking the assistant.
                </div>`;

            input.focus();

            return;
        }

        const response = findResponse(question);

        const finalResponse = response ??
            "I don't have enough information to answer that question accurately. I can help with appointments, medical records, clinic services, registration, account access and general healthcare guidance. For questions outside these areas, please contact reception or speak with a qualified healthcare professional.";

        const medicalQuestion =
            question.includes("headache") ||
            question.includes("flu") ||
            question.includes("cough") ||
            question.includes("fever") ||
            question.includes("sore throat") ||
            question.includes("stomach") ||
            question.includes("back pain") ||
            question.includes("toothache") ||
            question.includes("dizzy") ||
            question.includes("fatigue") ||
            question.includes("chest pain") ||
            question.includes("breathing") ||
            question.includes("stroke") ||
            question.includes("bleeding") ||
            question.includes("fracture") ||
            question.includes("broken bone") ||
            question.includes("heart attack");

        let disclaimer = "";

        if (medicalQuestion) {

            disclaimer =
                `<div class="alert alert-light border mt-3">
                    <small>
                        <strong>Medical Disclaimer:</strong>
                        This information is for general guidance only and
                        does not replace professional medical advice,
                        diagnosis or treatment. If you are experiencing
                        a medical emergency, seek immediate emergency care.
                    </small>
                </div>`;
        }

        responseContainer.innerHTML =
            `<div class="chat-message bot-message">

                <strong>Virtual Healthcare Assistant</strong>

                <p class="mb-0 mt-2">
                    ${finalResponse}
                </p>

                ${disclaimer}

            </div>`;
    }


    quickQuestions.forEach(function (button) {

        button.addEventListener("click", function () {

            input.value = button.dataset.question;

            getResponse();

        });

    });


    askButton.addEventListener("click", getResponse);


    input.addEventListener("keydown", function (event) {

        if (event.key === "Enter") {

            event.preventDefault();

            getResponse();

        }

    });

});