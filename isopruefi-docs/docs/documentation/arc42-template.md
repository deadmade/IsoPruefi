# arc42 Template

**About arc42**

arc42 is a template for documenting software and system architecture.

*Template Version 8.2 EN (AsciiDoc-based), January 2023*

Created and maintained by Dr. Peter Hruschka, Dr. Gernot Starke, and contributors. See [arc42.org](https://arc42.org).

> **Note:**  
> This version contains help and explanations for learning arc42 and understanding its concepts. For documenting your own system, use the *plain* version.

---

## 1. Introduction and Goals

Describes the relevant requirements and driving forces for software architects and the development team, including:

- Underlying business goals
- Essential features and functional requirements
- Quality goals for the architecture
- Relevant stakeholders and their expectations

### 1.1 Requirements Overview

**Contents:**  
Short description of functional requirements and driving forces. Reference requirements documents if available.

**Motivation:**  
From the end user's perspective, the system is created or modified to better support business activities or improve quality.

**Form:**  
Short textual description, possibly in tabular use-case format. Reference requirements documents if they exist.

See [Introduction and Goals](https://docs.arc42.org/section-1/) in the arc42 documentation.

### 1.2 Quality Goals

**Contents:**  
List the top three to five quality goals for the architecture that are most important to stakeholders.

**Motivation:**  
Knowing stakeholders' quality goals influences fundamental architectural decisions. Be concrete and avoid buzzwords.

**Form:**  
A table with quality goals and concrete scenarios, ordered by priority.

![Categories of Quality Requirements](images/01_2_iso-25010-topics-EN.drawio.png)

### 1.3 Stakeholders

**Contents:**  
Overview of system stakeholders (people, roles, organizations) who:

- Should know or be convinced of the architecture
- Work with the architecture or code
- Need the documentation for their work
- Make decisions about the system or its development

**Motivation:**  
Identify all parties involved or affected to avoid surprises later. Stakeholders determine the extent and detail of your work.

**Form:**  
Table with role names, contacts, and expectations.

| Role/Name   | Contact        | Expectations         |
|-------------|----------------|---------------------|
| *<Role-1>*  | *<Contact-1>*  | *<Expectation-1>*   |
| *<Role-2>*  | *<Contact-2>*  | *<Expectation-2>*   |

---

## 2. Architecture Constraints

**Contents:**  
Any requirement that constrains architectural or development decisions, possibly organization-wide.

**Motivation:**  
Architects must know where they are free to decide and where constraints apply.

**Form:**  
Simple tables of constraints with explanations. Subdivide into technical, organizational, or political constraints as needed.

See [Architecture Constraints](https://docs.arc42.org/section-2/).

---

## 3. Context and Scope

**Contents:**  
Defines your system's boundaries and its communication partners (neighboring systems, users). Specifies external interfaces.

**Motivation:**  
Understanding domain and technical interfaces is critical.

**Form:**  
- Context diagrams
- Lists of communication partners and interfaces

See [Context and Scope](https://docs.arc42.org/section-3/).

### 3.1 Business Context

**Contents:**  
List all communication partners (users, IT systems, etc.) with explanations of domain-specific inputs/outputs or interfaces.

**Motivation:**  
Stakeholders should understand data exchanged with the system's environment.

**Form:**  
Diagrams or tables showing the system as a black box and its domain interfaces.

**<Diagram or Table>**

**<Optional: Explanation of external domain interfaces>**

### 3.2 Technical Context

**Contents:**  
Technical interfaces (channels, transmission media) linking your system to its environment. Map domain-specific I/O to channels.

**Motivation:**  
Technical interfaces influence architectural decisions.

**Form:**  
E.g., UML deployment diagram and mapping table.

**<Diagram or Table>**

**<Optional: Explanation of technical interfaces>**

**<Mapping Input/Output to Channels>**

---

## 4. Solution Strategy

**Contents:**  
Summary of fundamental decisions and solution strategies shaping the architecture, including:

- Technology choices
- Top-level decomposition (patterns)
- Approaches to key quality goals
- Relevant organizational decisions

**Motivation:**  
These decisions are the foundation for detailed design and implementation.

**Form:**  
Keep explanations short. Motivate decisions based on problem statement, quality goals, and constraints.

See [Solution Strategy](https://docs.arc42.org/section-4/).

---

## 5. Building Block View

**Content:**  
Shows the static decomposition of the system into building blocks (modules, components, subsystems, etc.) and their dependencies.

**Motivation:**  
Maintain an overview of source code structure for communication and abstraction.

**Form:**  
Hierarchical collection of black boxes and white boxes.

![Hierarchy of building blocks](images/05_building_blocks-EN.png)

See [Building Block View](https://docs.arc42.org/section-5/).

### 5.1 Whitebox Overall System

Describe the decomposition of the overall system:

- Overview diagram
- Motivation for decomposition
- Black box descriptions of contained building blocks (table or list)
- (Optional) Important interfaces

**<Overview Diagram>**

**Motivation:**  
*<text explanation>*

**Contained Building Blocks:**  
*<Description of contained building blocks (black boxes)>*

**Important Interfaces:**  
*<Description of important interfaces>*

**Example Table:**

| Name            | Responsibility         |
|-----------------|-----------------------|
| *<black box 1>* | *<Text>*              |
| *<black box 2>* | *<Text>*              |

#### <Name black box 1>

- Purpose/Responsibility
- Interface(s)
- (Optional) Quality/Performance characteristics
- (Optional) Directory/File location
- (Optional) Fulfilled requirements
- (Optional) Open issues/problems/risks

*<Purpose/Responsibility>*  
*<Interface(s)>*  
*<Optional details as needed>*

#### <Name black box 2>

*<black box template>*

#### <Name black box n>

*<black box template>*

#### <Name interface 1>

...

#### <Name interface m>

### 5.2 Level 2

Specify the inner structure of selected building blocks from level 1 as white boxes.

#### White Box *<building block 1>*

*<white box template>*

#### White Box *<building block 2>*

*<white box template>*

...

#### White Box *<building block m>*

*<white box template>*

### 5.3 Level 3

Specify the inner structure of selected building blocks from level 2 as white boxes.

#### White Box <_building block x.1_>

*<white box template>*

#### White Box <_building block x.2_>

*<white box template>*

#### White Box <_building block y.1_>

*<white box template>*

---

## 6. Runtime View

**Contents:**  
Describes concrete behavior and interactions of building blocks in scenarios such as:

- Important use cases or features
- Interactions at critical external interfaces
- Operation and administration (start-up, shutdown)
- Error and exception scenarios

**Motivation:**  
Understand how building blocks perform and communicate at runtime.

**Form:**  
- Numbered steps
- Activity diagrams
- Sequence diagrams
- BPMN/EPCs
- State machines

See [Runtime View](https://docs.arc42.org/section-6/).

### <Runtime Scenario 1>

- *<Insert runtime diagram or textual description of the scenario>*
- *<Describe notable aspects of the interactions>*

### <Runtime Scenario 2>

...

### <Runtime Scenario n>

---

## 7. Deployment View

**Content:**  
Describes:

1. Technical infrastructure for system execution (locations, environments, computers, processors, channels, etc.)
2. Mapping of software building blocks to infrastructure elements

**Motivation:**  
Infrastructure influences the system and cross-cutting concepts.

**Form:**  
- UML deployment diagrams
- Other diagrams showing nodes and channels

See [Deployment View](https://docs.arc42.org/section-7/).

### 7.1 Infrastructure Level 1

Describe:

- Distribution to locations, environments, computers, etc.
- Justifications for deployment structure
- Quality/performance features
- Mapping of software artifacts to infrastructure

**<Overview Diagram>**

**Motivation:**  
*<explanation in text form>*

**Quality/Performance Features:**  
*<explanation in text form>*

**Mapping of Building Blocks to Infrastructure:**  
*<description of the mapping>*

### 7.2 Infrastructure Level 2

Include internal structure of selected infrastructure elements from level 1.

#### *<Infrastructure Element 1>*

*<diagram + explanation>*

#### *<Infrastructure Element 2>*

*<diagram + explanation>*

...

#### *<Infrastructure Element n>*

*<diagram + explanation>*

---

## 8. Cross-cutting Concepts

**Content:**  
Describes overall regulations and solution ideas relevant in multiple parts of the system, such as:

- Domain models
- Architecture/design patterns
- Technology usage rules
- Principal technical decisions
- Implementation rules

**Motivation:**  
Concepts ensure consistency and integrity across the architecture.

**Form:**  
- Concept papers
- Model excerpts or scenarios
- Sample implementations
- References to standard frameworks

**Structure (optional):**

- Domain concepts
- User Experience (UX) concepts
- Safety and security concepts
- Architecture/design patterns
- "Under-the-hood"
- Development concepts
- Operational concepts

![Possible topics for crosscutting concepts](images/08-concepts-EN.drawio.png)

See [Concepts](https://docs.arc42.org/section-8/).

### *<Concept 1>*

*<explanation>*

### *<Concept 2>*

*<explanation>*

...

### *<Concept n>*

*<explanation>*

---

## 9. Architecture Decisions

**Contents:**  
Document important, expensive, large-scale, or risky architecture decisions and their rationales.

**Motivation:**  
Stakeholders should be able to understand and retrace decisions.

**Form:**  
- ADRs ([Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions))
- List or table, ordered by importance
- Separate sections per decision

See [Architecture Decisions](https://docs.arc42.org/section-9/).

---

## 10. Quality Requirements

**Content:**  
All quality requirements as a quality tree with scenarios. Most important ones are in section 1.2.

**Motivation:**  
Quality requirements influence architectural decisions. Know what is important for each stakeholder.

See [Quality Requirements](https://docs.arc42.org/section-10/).

### 10.1 Quality Tree

**Content:**  
Quality tree (as in ATAM) with quality/evaluation scenarios as leaves.

**Motivation:**  
Tree structure with priorities provides an overview for many quality requirements.

**Form:**  
- Tree-like refinement of "quality"
- Mind map with quality categories as branches

Include links to scenarios in the next section.

### 10.2 Quality Scenarios

**Contents:**  
Concrete scenarios for quality requirements.

- Usage scenarios: System's runtime reaction to stimuli (e.g., performance)
- Change scenarios: Modifications to the system or environment

**Motivation:**  
Scenarios make quality requirements concrete and measurable.

**Form:**  
Tabular or free-form text.

---

## 11. Risks and Technical Debts

**Contents:**  
List of identified technical risks or debts, ordered by priority.

**Motivation:**  
Systematic detection and evaluation of risks and technical debts is essential for management.

**Form:**  
List of risks/debts, possibly with suggested mitigation measures.

See [Risks and Technical Debt](https://docs.arc42.org/section-11/).

---

## 12. Glossary

**Contents:**  
Most important domain and technical terms used by stakeholders.

**Motivation:**  
Define terms clearly to ensure shared understanding and avoid synonyms/homonyms.

**Form:**  
Table with columns for Term and Definition (add translations if needed).

See [Glossary](https://docs.arc42.org/section-12/).

| Term         | Definition         |
|--------------|-------------------|
| *<Term-1>*   | *<definition-1>*  |
| *<Term-2>*   | *<definition-2>*  |
