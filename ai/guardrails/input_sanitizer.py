"""
WayPoint AI — Input Sanitizer Guardrail (FR-AI-008).

Protects against prompt injection attacks by:
1. Stripping known injection patterns from user input
2. Truncating oversized input to MAX_INPUT_LENGTH (2000 chars)
3. Wrapping user-provided content in tagged delimiters so the LLM
   can distinguish system instructions from user content

Usage::

    from guardrails.input_sanitizer import sanitize_user_input, wrap_user_input

    clean = sanitize_user_input(raw_objective)
    wrapped = wrap_user_input(clean)
"""

import logging
import re

logger = logging.getLogger("waypoint.ai.guardrails.input_sanitizer")

# Maximum allowed input length (chars)
MAX_INPUT_LENGTH = 2000

# Known injection patterns to strip (case-insensitive)
# These patterns attempt to override system prompts or escape agent roles
_INJECTION_PATTERNS: list[re.Pattern] = [
    re.compile(r"ignore\s+(all\s+)?previous\s+instructions", re.IGNORECASE),
    re.compile(r"ignore\s+(all\s+)?prior\s+instructions", re.IGNORECASE),
    re.compile(r"disregard\s+(all\s+)?previous\s+instructions", re.IGNORECASE),
    re.compile(r"forget\s+(all\s+)?previous\s+instructions", re.IGNORECASE),
    re.compile(r"override\s+(all\s+)?system\s+prompt", re.IGNORECASE),
    re.compile(r"you\s+are\s+now\s+a", re.IGNORECASE),
    re.compile(r"pretend\s+you\s+are", re.IGNORECASE),
    re.compile(r"act\s+as\s+if\s+you\s+are", re.IGNORECASE),
    re.compile(r"new\s+instructions?:\s*", re.IGNORECASE),
    re.compile(r"system\s*:\s*", re.IGNORECASE),
    re.compile(r"\[system\]", re.IGNORECASE),
    re.compile(r"\[INST\]", re.IGNORECASE),
    re.compile(r"<\|im_start\|>", re.IGNORECASE),
    re.compile(r"<\|im_end\|>", re.IGNORECASE),
    re.compile(r"<\|system\|>", re.IGNORECASE),
    re.compile(r"<\|user\|>", re.IGNORECASE),
    re.compile(r"<\|assistant\|>", re.IGNORECASE),
    re.compile(r"```\s*system", re.IGNORECASE),
    re.compile(r"do\s+not\s+follow\s+your\s+rules", re.IGNORECASE),
    re.compile(r"reveal\s+your\s+(system\s+)?prompt", re.IGNORECASE),
    re.compile(r"show\s+me\s+your\s+(system\s+)?prompt", re.IGNORECASE),
    re.compile(r"what\s+are\s+your\s+instructions", re.IGNORECASE),
    re.compile(r"bypass\s+(all\s+)?safety", re.IGNORECASE),
    re.compile(r"jailbreak", re.IGNORECASE),
]


def sanitize_user_input(raw_input: str) -> str:
    """
    Sanitize user-provided input for prompt injection resistance.

    1. Strip known injection patterns
    2. Truncate to MAX_INPUT_LENGTH (2000 chars)
    3. Strip leading/trailing whitespace

    Args:
        raw_input: The raw user input string.

    Returns:
        Sanitized input string.
    """
    if not raw_input:
        return ""

    sanitized = raw_input
    patterns_stripped = 0

    for pattern in _INJECTION_PATTERNS:
        match = pattern.search(sanitized)
        if match:
            logger.warning(
                "Injection pattern stripped: '%s'", match.group()
            )
            sanitized = pattern.sub("", sanitized)
            patterns_stripped += 1

    if patterns_stripped > 0:
        logger.warning(
            "Stripped %d injection pattern(s) from input", patterns_stripped
        )

    # Truncate oversized input
    was_truncated = len(sanitized) > MAX_INPUT_LENGTH
    sanitized = sanitized[:MAX_INPUT_LENGTH].strip()

    if was_truncated:
        logger.warning(
            "Input truncated from %d to %d characters",
            len(raw_input),
            MAX_INPUT_LENGTH,
        )

    return sanitized


def wrap_user_input(sanitized_input: str) -> str:
    """
    Wrap sanitized user content in tagged delimiters.

    This creates a clear boundary between system instructions and
    user-provided content, making it harder for injection attacks
    to escape the user content context.

    Args:
        sanitized_input: Already-sanitized user input.

    Returns:
        Tagged user content string.
    """
    return (
        f"<user_input>\n"
        f"{sanitized_input}\n"
        f"</user_input>"
    )


def is_input_safe(raw_input: str) -> tuple[bool, list[str]]:
    """
    Check if input contains any known injection patterns.

    This is a non-destructive check — it doesn't modify the input,
    just reports whether it would be sanitized.

    Args:
        raw_input: The raw user input string.

    Returns:
        Tuple of (is_safe, list_of_matched_patterns).
    """
    if not raw_input:
        return True, []

    matched: list[str] = []
    for pattern in _INJECTION_PATTERNS:
        match = pattern.search(raw_input)
        if match:
            matched.append(match.group())

    return len(matched) == 0, matched
