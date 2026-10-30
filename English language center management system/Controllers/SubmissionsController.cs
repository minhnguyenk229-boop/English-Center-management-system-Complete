using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Controllers
{
    public class SubmissionsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: Submissions
        public ActionResult Index()
        {
            var submissions = db.Submissions.Include(s => s.Assignment).Include(s => s.Student);
            return View(submissions.ToList());
        }

        // GET: Submissions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Submission submission = db.Submissions.Find(id);
            if (submission == null)
            {
                return HttpNotFound();
            }
            return View(submission);
        }

        // GET: Submissions/Create
        public ActionResult Create()
        {
            ViewBag.AssignmentId = new SelectList(db.Assignments, "Id", "Title");
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username");
            return View();
        }

        // POST: Submissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,AssignmentId,StudentId,FileUrl,SubmittedAt,Grade")] Submission submission)
        {
            if (ModelState.IsValid)
            {
                db.Submissions.Add(submission);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AssignmentId = new SelectList(db.Assignments, "Id", "Title", submission.AssignmentId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", submission.StudentId);
            return View(submission);
        }

        // GET: Submissions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Submission submission = db.Submissions.Find(id);
            if (submission == null)
            {
                return HttpNotFound();
            }
            ViewBag.AssignmentId = new SelectList(db.Assignments, "Id", "Title", submission.AssignmentId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", submission.StudentId);
            return View(submission);
        }

        // POST: Submissions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,AssignmentId,StudentId,FileUrl,SubmittedAt,Grade")] Submission submission)
        {
            if (ModelState.IsValid)
            {
                db.Entry(submission).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AssignmentId = new SelectList(db.Assignments, "Id", "Title", submission.AssignmentId);
            ViewBag.StudentId = new SelectList(db.Users, "Id", "Username", submission.StudentId);
            return View(submission);
        }

        // GET: Submissions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Submission submission = db.Submissions.Find(id);
            if (submission == null)
            {
                return HttpNotFound();
            }
            return View(submission);
        }

        // POST: Submissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Submission submission = db.Submissions.Find(id);
            db.Submissions.Remove(submission);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
