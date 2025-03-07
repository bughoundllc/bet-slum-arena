RULES FOR THIS AI AGENT:
- You are to follow all rules presented here explicitly. If there is uncertainty, or the rule dictates so, you must halt processing and prompt the User for further action.
- You must NEVER modify this file.
- You are to follow the PROCEDURES section below as you do your work.
- Do NOT make assumptions about the content of data, only the shape. (for example, if i tell you i need a category, don't tell me about "religions") 
- Do NOT implement features that have not been asked for without explicit permission from the user.
- If you are creating or renaming a Data Structure, you MUST search the project to verify you are not introducing a duplicate into the namespace.
- If you come across a configuration file with sensitive data in it (such as a connection string), DO NOT remove the data. However, you SHOULD add a comment or note and make mention of it in your summary to the user.

PROCEDURES TO FOLLOW:
- If the files mentioned below do not exist, create and populate them.
- The FIRST thing you should always do in a session is ingest information about the project found in `agent-info.md`. Technical documentation to supplement is found in `technical-documentation.md`. 
- If you make ANY addition or change to a Data Structure (especially any new files or deletions), you must verify before checkout that `technical-documentation.md` is up to date with your latest changes.
- Once you are finished with a Task, commit the changes to the git project. If no project exists, initialize one.
- If you're corrected on a bad assumption, make note of your misunderstanding and the correction in `agent-info.md` to prevent further mistakes.
- Comment your code thoroughly.
- When committing a change to Git, prepend the tag "[AI:CLAUDE]".
- Be sure that you are committing changes in the correct directory. DO NOT commit in the main directory if you are in a solution of projects; this is a master project and will not track changes.
- When finishing a task, you should ALWAYS run the modified application. If there are any build errors, troubleshoot. Halt and alert the user if unable to fix in a timely manner.
- When committing a change to Git, check if the project is a submodule of any other projects in the solution. If so, cascade the update to them and commit those as well.