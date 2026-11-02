package main

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}
type Issue struct {
	ID          string   `json:"id"`
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	Status      string   `json:"status"`
	Position    Position `json:"position"`
	CreatedAt   string   `json:"createdAt"`
	UpdatedAt   string   `json:"updatedAt"`
}
type Store struct {
	mu     sync.Mutex
	path   string
	issues []Issue
}

func openStore(path string) (*Store, error) {
	s := &Store{path: path, issues: []Issue{}}
	b, err := os.ReadFile(path)
	if errors.Is(err, os.ErrNotExist) {
		return s, nil
	}
	if err != nil {
		return nil, err
	}
	if err = json.Unmarshal(b, &s.issues); err != nil {
		return nil, fmt.Errorf("invalid data file (preserved): %w", err)
	}
	if s.issues == nil {
		return nil, errors.New("data file must contain a JSON array")
	}
	return s, nil
}

// Caller holds the lock. Only publish the new snapshot after disk persistence succeeds.
func (s *Store) save(next []Issue) error {
	if err := os.MkdirAll(filepath.Dir(s.path), 0700); err != nil {
		return err
	}
	b, err := json.MarshalIndent(next, "", "  ")
	if err != nil {
		return err
	}
	f, err := os.CreateTemp(filepath.Dir(s.path), ".issues-*.tmp")
	if err != nil {
		return err
	}
	defer os.Remove(f.Name())
	if _, err = f.Write(b); err != nil {
		f.Close()
		return err
	}
	if err = f.Sync(); err != nil {
		f.Close()
		return err
	}
	if err = f.Close(); err != nil {
		return err
	}
	if err = os.Rename(f.Name(), s.path); err != nil {
		return err
	}
	s.issues = next
	return nil
}
func respond(w http.ResponseWriter, code int, value any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(code)
	_ = json.NewEncoder(w).Encode(value)
}
func fail(w http.ResponseWriter, code int, message string) {
	respond(w, code, map[string]string{"error": message})
}
func decode(w http.ResponseWriter, r *http.Request, v any) bool {
	r.Body = http.MaxBytesReader(w, r.Body, 64*1024)
	d := json.NewDecoder(r.Body)
	d.DisallowUnknownFields()
	if err := d.Decode(v); err != nil {
		fail(w, 400, "invalid JSON: "+err.Error())
		return false
	}
	if err := d.Decode(new(any)); err != io.EOF {
		fail(w, 400, "body must contain exactly one JSON object")
		return false
	}
	return true
}
func oneOf(v string, values ...string) bool {
	for _, x := range values {
		if v == x {
			return true
		}
	}
	return false
}
func handler(s *Store, origins string) http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) { respond(w, 200, map[string]string{"status": "ok"}) })
	mux.HandleFunc("GET /api/issues", func(w http.ResponseWriter, r *http.Request) {
		s.mu.Lock()
		defer s.mu.Unlock()
		respond(w, 200, s.issues)
	})
	mux.HandleFunc("POST /api/issues", func(w http.ResponseWriter, r *http.Request) {
		var input struct {
			Title       string `json:"title"`
			Description string `json:"description"`
			Priority    string `json:"priority"`
			Position    *struct {
				X *float64 `json:"x"`
				Y *float64 `json:"y"`
				Z *float64 `json:"z"`
			} `json:"position"`
		}
		if !decode(w, r, &input) {
			return
		}
		input.Title = strings.TrimSpace(input.Title)
		if input.Title == "" {
			fail(w, 400, "title is required")
			return
		}
		if !oneOf(input.Priority, "low", "medium", "high") {
			fail(w, 400, "priority must be low, medium or high")
			return
		}
		if input.Position == nil || input.Position.X == nil || input.Position.Y == nil || input.Position.Z == nil {
			fail(w, 400, "position.x/y/z are required numbers")
			return
		}
		id := make([]byte, 16)
		if _, err := rand.Read(id); err != nil {
			fail(w, 500, "cannot generate id")
			return
		}
		now := time.Now().UTC().Format(time.RFC3339Nano)
		item := Issue{hex.EncodeToString(id), input.Title, input.Description, input.Priority, "open", Position{*input.Position.X, *input.Position.Y, *input.Position.Z}, now, now}
		s.mu.Lock()
		defer s.mu.Unlock()
		next := append(append([]Issue{}, s.issues...), item)
		if err := s.save(next); err != nil {
			log.Printf("save: %v", err)
			fail(w, 500, "failed to persist issue")
			return
		}
		respond(w, 201, item)
	})
	mux.HandleFunc("PATCH /api/issues/{id}/status", func(w http.ResponseWriter, r *http.Request) {
		var input struct {
			Status string `json:"status"`
		}
		if !decode(w, r, &input) {
			return
		}
		if !oneOf(input.Status, "open", "in_progress", "resolved") {
			fail(w, 400, "status must be open, in_progress or resolved")
			return
		}
		s.mu.Lock()
		defer s.mu.Unlock()
		for i, item := range s.issues {
			if item.ID == r.PathValue("id") {
				next := append([]Issue{}, s.issues...)
				next[i].Status = input.Status
				next[i].UpdatedAt = time.Now().UTC().Format(time.RFC3339Nano)
				if err := s.save(next); err != nil {
					log.Printf("save: %v", err)
					fail(w, 500, "failed to persist status")
					return
				}
				respond(w, 200, next[i])
				return
			}
		}
		fail(w, 404, "issue not found")
	})
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		origin := r.Header.Get("Origin")
		if origin != "" {
			w.Header().Add("Vary", "Origin")
			for _, allowed := range strings.Split(origins, ",") {
				if origin == strings.TrimSpace(allowed) {
					w.Header().Set("Access-Control-Allow-Origin", origin)
					break
				}
			}
		}
		if r.Method == http.MethodOptions {
			w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PATCH, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
			w.WriteHeader(204)
			return
		}
		mux.ServeHTTP(w, r)
	})
}
func env(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
func main() {
	path, err := filepath.Abs(env("DATA_FILE", "data/issues.json"))
	if err != nil {
		log.Fatal(err)
	}
	s, err := openStore(path)
	if err != nil {
		log.Fatal(err)
	}
	addr := env("ADDR", ":8080")
	log.Printf("listening on %s; data: %s", addr, path)
	server := &http.Server{Addr: addr, Handler: handler(s, env("CORS_ORIGINS", "http://localhost:5173,http://127.0.0.1:5173")), ReadHeaderTimeout: 5 * time.Second, ReadTimeout: 10 * time.Second, WriteTimeout: 15 * time.Second, IdleTimeout: 60 * time.Second}
	log.Fatal(server.ListenAndServe())
}
