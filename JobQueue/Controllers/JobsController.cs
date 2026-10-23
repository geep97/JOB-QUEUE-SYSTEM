using Microsoft.AspNetCore.Mvc;
using JobQueue.Data;
using JobQueue.Models;
using JobQueue.Services;
using JobQueue.DTOs;




namespace JobQueue.Controllers
{

    [ApiController]

    [Route("api/[controller]")]

    public class JobsController : ControllerBase
    {
        private readonly IJobsService _jobsService;
        public JobsController(IJobsService jobsService)
        {
            _jobsService = jobsService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobDto jobCreateDto)
        {
            var job = await _jobsService.CreateJobAsync(jobCreateDto);
            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllJobs()
        {
            var jobs = await _jobsService.GetAllJobsAsync();
            return Ok(jobs);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var job = await _jobsService.GetJobByIdAsync(id);
            if (job == null)
            {
                return NotFound();
            }
            return Ok(job);
        }


        [HttpPut("{id}/status")])]
        public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] UpdateJobStatusDto updateJobStatusDto)
        {
            var success = await _jobsService.UpdateJobStatusAsync(id, updateJobStatusDto.Status, updateJobStatusDto.ErrorMessage);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
    }

}
