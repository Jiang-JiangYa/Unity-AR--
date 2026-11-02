package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"sync"
	"testing"
)

const valid = `{"title":"入口墙面破损","description":"左侧墙体存在裂缝","priority":"high","position":{"x":0.42,"y":0.03,"z":1.26}}`

func call(h http.Handler, method, path, body string) *httptest.ResponseRecorder {
	r := httptest.NewRequest(method, path, bytes.NewBufferString(body))
	r.Header.Set("Content-Type", "application/json")
	w := httptest.NewRecorder()
	h.ServeHTTP(w, r)
	return w
}
func setup(t *testing.T) (*Store, http.Handler) {
	t.Helper()
	s, err := openStore(filepath.Join(t.TempDir(), "issues.json"))
	if err != nil {
		t.Fatal(err)
	}
	return s, handler(s, "http://localhost:5173")
}
func TestLifecycleAndRestart(t *testing.T) {
	s, h := setup(t)
	if w := call(h, "GET", "/health", ""); w.Code != 200 {
		t.Fatal(w.Code)
	}
	if w := call(h, "GET", "/api/issues", ""); w.Body.String() != "[]\n" {
		t.Fatal(w.Body.String())
	}
	w := call(h, "POST", "/api/issues", valid)
	if w.Code != 201 {
		t.Fatal(w.Code, w.Body.String())
	}
	var issue Issue
	if err := json.Unmarshal(w.Body.Bytes(), &issue); err != nil {
		t.Fatal(err)
	}
	if issue.ID == "" || issue.Status != "open" || issue.CreatedAt == "" || issue.Position.X != .42 {
		t.Fatal(issue)
	}
	for _, status := range []string{"in_progress", "resolved", "open"} {
		w = call(h, "PATCH", "/api/issues/"+issue.ID+"/status", `{"status":"`+status+`"}`)
		if w.Code != 200 {
			t.Fatal(w.Code, w.Body.String())
		}
	}
	restarted, err := openStore(s.path)
	if err != nil {
		t.Fatal(err)
	}
	if len(restarted.issues) != 1 || restarted.issues[0].ID != issue.ID || restarted.issues[0].Status != "open" {
		t.Fatal(restarted.issues)
	}
	if restarted.issues[0].UpdatedAt == issue.UpdatedAt {
		t.Fatal("updatedAt did not change")
	}
}
func TestValidation(t *testing.T) {
	_, h := setup(t)
	for _, body := range []string{
		`{}`, `null`, `{"title":"  "}`, `{"title":"x","priority":"urgent"}`,
		`{"title":"x","priority":"low"}`, `{"title":"x","priority":"low","position":{"x":0,"y":0}}`,
		`{"title":"x","priority":"low","position":{"x":null,"y":0,"z":0}}`,
		`{"title":"x","priority":"low","position":{"x":"bad","y":0,"z":0}}`,
		valid + ` {}`, `{`,
	} {
		w := call(h, "POST", "/api/issues", body)
		if w.Code != 400 {
			t.Errorf("body %s: got %d", body, w.Code)
		}
		var e map[string]string
		_ = json.Unmarshal(w.Body.Bytes(), &e)
		if e["error"] == "" {
			t.Error("missing error")
		}
	}
	if w := call(h, "PATCH", "/api/issues/missing/status", `{"status":"done"}`); w.Code != 400 {
		t.Fatal(w.Code)
	}
	if w := call(h, "PATCH", "/api/issues/missing/status", `{"status":"resolved"}`); w.Code != 404 {
		t.Fatal(w.Code)
	}
}
func TestCORS(t *testing.T) {
	_, h := setup(t)
	for _, origin := range []string{"http://localhost:5173", "https://untrusted.invalid"} {
		r := httptest.NewRequest("OPTIONS", "/api/issues/x/status", nil)
		r.Header.Set("Origin", origin)
		r.Header.Set("Access-Control-Request-Method", "PATCH")
		w := httptest.NewRecorder()
		h.ServeHTTP(w, r)
		if w.Code != 204 {
			t.Fatal(w.Code)
		}
		allowed := w.Header().Get("Access-Control-Allow-Origin")
		if (origin == "http://localhost:5173") != (allowed == origin) {
			t.Fatal(allowed)
		}
	}
}
func TestConcurrentWrites(t *testing.T) {
	s, h := setup(t)
	var wg sync.WaitGroup
	for i := 0; i < 24; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			w := call(h, "POST", "/api/issues", valid)
			if w.Code != 201 {
				t.Errorf("create: %d %s", w.Code, w.Body.String())
			}
		}()
	}
	wg.Wait()
	restarted, err := openStore(s.path)
	if err != nil {
		t.Fatal(err)
	}
	if len(restarted.issues) != 24 {
		t.Fatal(len(restarted.issues))
	}
	ids := map[string]bool{}
	for _, i := range restarted.issues {
		if ids[i.ID] {
			t.Fatal("duplicate id")
		}
		ids[i.ID] = true
	}
}
func TestPersistenceFailureDoesNotPublish(t *testing.T) {
	s, h := setup(t)
	w := call(h, "POST", "/api/issues", valid)
	if w.Code != 201 {
		t.Fatal(w.Code)
	}
	var issue Issue
	_ = json.Unmarshal(w.Body.Bytes(), &issue)
	blocker := filepath.Join(t.TempDir(), "file-not-directory")
	if err := os.WriteFile(blocker, []byte("x"), 0600); err != nil {
		t.Fatal(err)
	}
	s.path = filepath.Join(blocker, "issues.json")
	if w := call(h, "POST", "/api/issues", valid); w.Code != 500 {
		t.Fatal(w.Code)
	}
	if len(s.issues) != 1 {
		t.Fatal("failed create published")
	}
	if w := call(h, "PATCH", fmt.Sprintf("/api/issues/%s/status", issue.ID), `{"status":"resolved"}`); w.Code != 500 {
		t.Fatal(w.Code)
	}
	if s.issues[0].Status != "open" {
		t.Fatal("failed update published")
	}
}
func TestCorruptFileIsPreserved(t *testing.T) {
	path := filepath.Join(t.TempDir(), "issues.json")
	if err := os.WriteFile(path, []byte("broken"), 0600); err != nil {
		t.Fatal(err)
	}
	if _, err := openStore(path); err == nil {
		t.Fatal("expected startup failure")
	}
	b, _ := os.ReadFile(path)
	if string(b) != "broken" {
		t.Fatal("file modified")
	}
}
