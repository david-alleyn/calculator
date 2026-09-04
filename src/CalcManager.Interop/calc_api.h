// The Linux interop surface for the Calculator engine (C ABI).
// Consumed from .NET via source-generated P/Invoke.

#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Opaque handle to a calculator engine session.
typedef struct CalcEngineSession CalcEngineSession;

// Status codes returned by the API.
#define CALC_OK 0
#define CALC_ERR_INVALID_ARGUMENT 1
#define CALC_ERR_NO_STATE 2

// Calculator modes (must match CalculationManager::CalculatorMode).
#define CALC_MODE_STANDARD 0
#define CALC_MODE_SCIENTIFIC 1

// Creates a new engine session in the given mode. Returns CALC_OK on success and
// stores the session handle in outSession. The caller owns the session and must
// release it with calc_session_destroy.
int32_t calc_session_create(int32_t mode, CalcEngineSession** outSession);

// Destroys a session created by calc_session_create.
void calc_session_destroy(CalcEngineSession* session);

// Sends a calculator command. commandId must be one of the
// CalculationManager::Command numeric values (see src/CalcManager/Command.h).
int32_t calc_session_send_command(CalcEngineSession* session, uint32_t commandId);

// Returns the current primary display value as UTF-8.
// The returned pointer is owned by the session and remains valid until the
// next call into the same session. May return NULL if no state is available.
const char* calc_session_get_primary_display(CalcEngineSession* session);

// Returns the current expression display as UTF-8, or NULL if empty.
// Pointer lifetime is the same as calc_session_get_primary_display.
const char* calc_session_get_expression_display(CalcEngineSession* session);

// Returns true when the session is in an error state (e.g. divide by zero).
int32_t calc_session_is_in_error(CalcEngineSession* session);

// Returns the number of history items in the session.
int32_t calc_session_get_history_length(CalcEngineSession* session);

// Returns history item at index as "expression = result" (UTF-8), or NULL if
// the index is out of range. Pointer lifetime is the same as above.
const char* calc_session_get_history_entry(CalcEngineSession* session, uint32_t index);

// Clears the session history.
void calc_session_clear_history(CalcEngineSession* session);

// Returns the number of entered parentheses (0 when none are open).
int32_t calc_session_get_parenthesis_count(CalcEngineSession* session);

#ifdef __cplusplus
}
#endif
