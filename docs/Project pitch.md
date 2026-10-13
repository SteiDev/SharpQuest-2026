## Theme: Game Library

### Overview
> *"A personal game library that keeps you honest."*

**Backlog Auditor** is a desktop, web, and database application designed to track the games you own, what you have actually played, and the uncomfortable truth about your unplayed collection. It takes a personal library and turns it into numbers you might not want to see: the true cost of your backlog, the financial waste of abandoned games, and the reality of your gaming habits.

### The Problem
Gamers buy games faster than they finish them. Between Steam sales, physical copies, and monthly subscription services, games pile up in digital and physical spaces. The "backlog" becomes an invisible weight. Players lose track of what they own, forget how much money is tied up in unplayed titles, and routinely abandon genres they thought they loved. There is no simple, personal dashboard to confront these habits.

### The Core Feature: "Backlog Shame"
The primary feature built across the semester is **Backlog Shame**. 

For every game marked with the `Backlog` status, the user inputs an estimated number of hours required to beat it. The application calculates and confronts the user with:
*   **Total Backlog Hours:** The sheer volume of time required to clear the current queue.
*   **Months to Clear:** A reality check based on the user's actual, historical weekly play rate.
*   **Missing Estimates:** Highlighting games sitting in the backlog that the user hasn't even bothered to evaluate.
*   **Platform/Genre Guilt:** Identifying which platform (e.g., PC vs. Switch) or genre contributes the most to the backlog.

### Key Questions the App Answers
1. How many hours is my backlog?
2. What did each hour of fun actually cost me?
3. Which genre do I always give up on?
4. What subscriptions am I paying for but never playing?
5. What is the highest-rated game I own but never finished?

### Domain & Data Strategy
The domain models a real, personal collection rather than random filler data. 
*   **Core Entities:** `Game` (Id, title, platform, genre, release year, hours played, price paid, status, optional rating), `Developer` (Name, country, founded year).
*   **Enums:** `Status` (Backlog, Playing, Completed, Abandoned) and `Platform`.
*   **The Dataset:** At least **30 real games** from at least **6 different developers**, utilizing every status and spanning at least **3 platforms**. 
*   **External Data:** Optional integration with the [RAWG API](https://rawg.io/apidocs) to fetch metadata (genres, release dates) while keeping personal playtime and prices strictly local and private.
