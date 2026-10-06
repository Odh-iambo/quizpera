# Quizpera Question Engine

## Purpose

The Quizpera Question Engine is responsible for defining, storing,
retrieving, presenting, and evaluating questions used across Quizpera
assessment experiences.

The engine must support the current requirements for:

- Custom Exams
- CATs
- Readiness Assessments
- Standalone Questions
- Tutored CATs

The architecture must remain extensible so additional assessment
and question capabilities can be introduced without redesigning
the entire system.

## Reference Basis

QLexNursing is used as a reference for understanding existing
question and assessment capabilities.

Quizpera is an independent system and will implement its own
domain model, database, API, and assessment engine.

The reference does not define Quizpera's implementation.

## Core Separation

Quizpera separates question definitions from student responses.

### Question Definition

A question represents reusable educational content and its
evaluation rules.

It may contain:

- Question stem/content
- Question type
- Response/options
- Correct response
- Rationale
- Difficulty
- Domain/subject classification
- Client-needs classification
- Additional metadata

### Student Response

A student response represents a learner's interaction with a
question during an assessment or standalone practice.

It may contain:

- Student
- Question
- Selected/submitted response
- Correctness result
- Timing information
- Interaction information
- Assessment/session context

A question must not contain a student's individual response.

## Design Principle

Questions are reusable content.

Responses belong to individual learners and individual
assessment sessions.

This separation allows the same question to participate in:

- Multiple custom exams
- CAT sessions
- Readiness assessments
- Standalone practice
- Tutored CAT sessions

without duplicating the question itself.

---

## Question Types

Question types describe the interaction and evaluation behavior
of a question.

They are separate from question classification such as:

- Program
- Domain
- Subject
- Client Need
- Topic
- Difficulty

The question type determines how a learner is expected to respond
and how the response can be evaluated.

### Confirmed Reference Capability

The QLexNursing question interface reviewed during research
demonstrates a question containing multiple answer options where
the learner selects a response and the system identifies a
correct answer.

Quizpera will represent this capability as:

- `SingleChoice`

This name describes Quizpera's own implementation and does not
assume the internal naming used by QLexNursing.

### Extensibility

The Quizpera question system must allow additional question types
to be introduced without restructuring the core Question entity.

Potential future question types must not be added merely because
they are common in other examination platforms. They should be
introduced when they are confirmed by requirements or when the
Quizpera product explicitly decides to support them.

Question type is therefore treated as a first-class concept
rather than hard-coded throughout the application.
