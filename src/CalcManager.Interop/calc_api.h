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

// Returns the number of history items in the session. mode: -1 for the current
// engine history, 0 for standard mode, 1 for scientific mode.
int32_t calc_session_get_history_length(CalcEngineSession* session);
int32_t calc_session_get_history_length_for_mode(CalcEngineSession* session, int32_t mode);

// Removes the history item at index. Returns non-zero on success.
int32_t calc_session_remove_history_item(CalcEngineSession* session, uint32_t index);

// Recomputes the memorized-numbers display strings and pushes them through the
// memorized numbers callback (mirrors CalculatorManager::SetMemorizedNumbersString).
void calc_session_set_memorized_numbers_string(CalcEngineSession* session);

// Returns history item at index as "expression = result" (UTF-8), or NULL if
// the index is out of range. Pointer lifetime is the same as above.
const char* calc_session_get_history_entry(CalcEngineSession* session, uint32_t index);

// Clears the session history.
void calc_session_clear_history(CalcEngineSession* session);

// Clears the current engine's history (mirrors CalculatorManager::ClearHistory,
// which is more than an empty SetHistoryItems).
void calc_session_clear_current_history(CalcEngineSession* session);

// Returns the number of entered parentheses (0 when none are open).
int32_t calc_session_get_parenthesis_count(CalcEngineSession* session);

// Resets the session. When clearMemory is non-zero the memory slots are
// cleared as well (mirrors CalculatorManager::Reset(bool)).
int32_t calc_session_reset(CalcEngineSession* session, int32_t clearMemory);

// Mode switching (mirrors CalculatorManager::Set*Mode).
void calc_session_set_standard_mode(CalcEngineSession* session);
void calc_session_set_scientific_mode(CalcEngineSession* session);
void calc_session_set_programmer_mode(CalcEngineSession* session);

// Radix / precision control (programmer mode).
void calc_session_set_radix(CalcEngineSession* session, int32_t radixType);
void calc_session_set_precision(CalcEngineSession* session, int32_t precision);
void calc_session_update_max_int_digits(CalcEngineSession* session);
const char* calc_session_get_result_for_radix(CalcEngineSession* session, uint32_t radix, int32_t precision, int32_t groupDigitsPerRadix);

// Returns the decimal separator character used by the engine (as a code point).
int32_t calc_session_get_decimal_separator(CalcEngineSession* session);

// Engine state queries.
int32_t calc_session_is_engine_recording(CalcEngineSession* session);
int32_t calc_session_is_input_empty(CalcEngineSession* session);
int32_t calc_session_get_current_degree_mode(CalcEngineSession* session);
void calc_session_set_in_history_load_mode(CalcEngineSession* session, int32_t isHistoryItemLoadMode);

// Memory slots.
void calc_session_memorize_number(CalcEngineSession* session);
void calc_session_memorized_number_load(CalcEngineSession* session, uint32_t index);
void calc_session_memorized_number_add(CalcEngineSession* session, uint32_t index);
void calc_session_memorized_number_subtract(CalcEngineSession* session, uint32_t index);
void calc_session_memorized_number_clear(CalcEngineSession* session, uint32_t index);
void calc_session_memorized_number_clear_all(CalcEngineSession* session);

// Returns the memorized numbers joined with '\n' (UTF-8), or an empty string
// when no memory slots are in use.
const char* calc_session_get_memorized_numbers(CalcEngineSession* session);

// History item with tokens and command tree. Serialized as a flat blob:
//   u32 tokenCount; per token: u32 utf8len, utf8 value, s32 commandIndex;
//   u32 commandCount; per command: u8 type, s32 command, u8 flags, u32 subCount,
//   s32 subcommands...; u32 expressionLen, utf8 expression, u32 resultLen, utf8 result.
// Command type: 0 unary, 1 binary, 2 operand, 3 parentheses.
// Command flags bit 0: isNegative, bit 1: isDecimalPresent, bit 2: isSciFmt.
// mode selects the history source: -1 for the current engine history
// (GetHistoryItems), 0 for standard mode history, 1 for scientific mode.
uint32_t calc_session_get_history_item_blob_size(CalcEngineSession* session, int32_t mode, uint32_t index);
int32_t calc_session_get_history_item_blob(CalcEngineSession* session, int32_t mode, uint32_t index, uint8_t* buffer, uint32_t bufferSize);

// Sets the full history from a serialized blob: u32 itemCount followed by the
// history items in the same format as above.
int32_t calc_session_set_history_items(CalcEngineSession* session, const uint8_t* blob, uint32_t blobSize);

// Current display expression command tree (the snapshot used to restore
// in-progress calculations). Same command serialization as above, framed with
// a leading u32 commandCount and without tokens or strings.
uint32_t calc_session_get_display_commands_blob_size(CalcEngineSession* session);
int32_t calc_session_get_display_commands_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize);

// Expression display tokens and commands as reported by the last
// SetExpressionDisplay callback. Tokens: u32 count; per token u32 utf8len,
// utf8 value, s32 commandIndex. Commands use the command serialization above
// prefixed with u32 commandCount.
uint32_t calc_session_get_expression_tokens_blob_size(CalcEngineSession* session);
int32_t calc_session_get_expression_tokens_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize);
uint32_t calc_session_get_expression_commands_blob_size(CalcEngineSession* session);
int32_t calc_session_get_expression_commands_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize);

#ifdef __cplusplus
}
#endif
